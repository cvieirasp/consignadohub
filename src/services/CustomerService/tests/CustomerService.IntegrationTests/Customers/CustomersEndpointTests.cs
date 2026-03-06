using System.Net;
using System.Net.Http.Json;
using ConsignadoHub.BuildingBlocks.Auth;
using ConsignadoHub.BuildingBlocks.Results;
using CustomerService.Application.DTOs;
using CustomerService.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace CustomerService.IntegrationTests.Customers;

[Trait("Category", "Integration")]
public sealed class CustomersEndpointTests(CustomerApiFactory factory)
    : IClassFixture<CustomerApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly HttpClient _analystClient = factory.CreateClientWithRole(Roles.Analyst);

    // Known-valid CPFs (check digits verified), one per test to prevent collisions
    private const string CpfCreate     = "529.982.247-25"; // normalized: 52998224725
    private const string CpfGetById    = "111.444.777-35"; // normalized: 11144477735
    private const string CpfDuplicate  = "123.456.789-09"; // normalized: 12345678909
    private const string CpfGetByCpf   = "987.654.321-00"; // normalized: 98765432100
    private const string CpfUpdate     = "714.447.693-91"; // normalized: 71444769391
    private const string CpfDeactivate = "123.456.780-62"; // normalized: 12345678062
    private const string CpfSearch     = "234.567.890-92"; // normalized: 23456789092

    [Fact]
    public async Task POST_CreateCustomer_Returns201_WithIdInBody()
    {
        var response = await _client.PostAsJsonAsync("/v1/customers", ValidInput(CpfCreate));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GET_GetCustomerById_Returns200_WithCorrectData()
    {
        var createResponse = await _client.PostAsJsonAsync("/v1/customers", ValidInput(CpfGetById));
        var id = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var response = await _client.GetAsync($"/v1/customers/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<CustomerDto>();
        dto!.Id.Should().Be(id);
        dto.Cpf.Should().Be("11144477735");
        dto.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GET_GetCustomerById_Returns404_WhenCustomerDoesNotExist()
    {
        var response = await _client.GetAsync($"/v1/customers/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_CreateCustomer_Returns409_WhenCpfAlreadyRegistered()
    {
        await _client.PostAsJsonAsync("/v1/customers", ValidInput(CpfDuplicate));

        var duplicate = await _client.PostAsJsonAsync("/v1/customers", ValidInput(CpfDuplicate));

        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GET_GetCustomerByCpf_Returns200_WhenCustomerExists()
    {
        await _client.PostAsJsonAsync("/v1/customers", ValidInput(CpfGetByCpf));

        var response = await _client.GetAsync("/v1/customers/cpf/98765432100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<CustomerDto>();
        dto!.Cpf.Should().Be("98765432100");
    }

    [Fact]
    public async Task PUT_UpdateCustomer_Returns204_WhenCustomerExists()
    {
        var createResponse = await _client.PostAsJsonAsync("/v1/customers", ValidInput(CpfUpdate));
        var id = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var updateInput = new { FullName = "Updated Name", Email = "updated@example.com", Phone = "11988887777" };
        var response = await _client.PutAsJsonAsync($"/v1/customers/{id}", updateInput);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DELETE_DeactivateCustomer_Returns204_WhenCustomerIsActive()
    {
        var createResponse = await _client.PostAsJsonAsync("/v1/customers", ValidInput(CpfDeactivate));
        var id = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var response = await _client.DeleteAsync($"/v1/customers/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GET_SearchCustomers_Returns200_WithMatchingCustomer()
    {
        await _client.PostAsJsonAsync("/v1/customers",
            new { FullName = "Maria Buscavel Silva", Cpf = CpfSearch, Email = "maria@example.com", Phone = "11911112222", BirthDate = "1985-06-20" });

        var response = await _client.GetAsync("/v1/customers?name=Buscavel&page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<CustomerSummaryDto>>();
        result!.Items.Should().Contain(c => c.FullName == "Maria Buscavel Silva");
    }

    // --- RBAC: analyst role (AnalystOrAdmin) endpoints ---

    [Fact]
    public async Task GET_GetCustomerById_Returns200_ForAnalyst()
    {
        var createResponse = await _client.PostAsJsonAsync("/v1/customers", ValidInput("295.379.955-93"));
        var id = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var response = await _analystClient.GetAsync($"/v1/customers/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_SearchCustomers_Returns200_ForAnalyst()
    {
        var response = await _analystClient.GetAsync("/v1/customers?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // --- RBAC: analyst is forbidden on admin-only endpoints ---

    [Fact]
    public async Task POST_CreateCustomer_Returns403_ForAnalyst()
    {
        var response = await _analystClient.PostAsJsonAsync("/v1/customers", ValidInput("748.632.740-28"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PUT_UpdateCustomer_Returns403_ForAnalyst()
    {
        var createResponse = await _client.PostAsJsonAsync("/v1/customers", ValidInput("867.491.130-79"));
        var id = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var updateInput = new { FullName = "Should Fail", Email = "fail@example.com", Phone = "11999998888" };
        var response = await _analystClient.PutAsJsonAsync($"/v1/customers/{id}", updateInput);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DELETE_DeactivateCustomer_Returns403_ForAnalyst()
    {
        var createResponse = await _client.PostAsJsonAsync("/v1/customers", ValidInput("301.468.580-18"));
        var id = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var response = await _analystClient.DeleteAsync($"/v1/customers/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static object ValidInput(string cpf) => new
    {
        FullName = $"Test User {cpf[..3]}",
        Cpf = cpf,
        Email = $"test{cpf.Replace(".", "")[..5]}@example.com",
        Phone = "11999999999",
        BirthDate = "1990-05-10"
    };
}
