using System.Net;
using RaceDay.API.Dtos;
using RaceDay.API.Models;
using RaceDay.API.Tests.Infrastructure;

namespace RaceDay.API.Tests.Api;

/// <summary>Organisers defining age and distance categories for their events.</summary>
public class CategoryTests : ApiTestBase
{
    [Fact]
    public async Task Organiser_AddsAgeCategory_Returns201()
    {
        var response = await PostJson(await AsOrganiser(), $"/api/events/{Data.UpcomingEventId}/categories",
            new { categoryName = "Senior", categoryType = "Age" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var category = await ReadAsync<CategoryResponse>(response);
        Assert.Equal("Senior", category.CategoryName);
        Assert.Equal(CategoryType.Age, category.CategoryType);
        Assert.Equal(Data.UpcomingEventId, category.EventId);
    }

    [Fact]
    public async Task Organiser_AddsDuplicateCategoryName_Returns409()
    {
        var response = await PostJson(await AsOrganiser(), $"/api/events/{Data.UpcomingEventId}/categories",
            new { categoryName = "10km", categoryType = "Distance" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_AddsCategoryToAnotherOrganisersEvent_Returns403()
    {
        var response = await PostJson(await AsOtherOrganiser(), $"/api/events/{Data.UpcomingEventId}/categories",
            new { categoryName = "21km", categoryType = "Distance" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_UpdatesCategory_Returns200()
    {
        var response = await PutJson(await AsOrganiser(), $"/api/categories/{Data.Upcoming10kCategoryId}",
            new { categoryName = "10km Open", categoryType = "Distance" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("10km Open", (await ReadAsync<CategoryResponse>(response)).CategoryName);
    }

    [Fact]
    public async Task Organiser_DeletesUnusedCategory_Returns204()
    {
        var response = await (await AsOrganiser()).DeleteAsync($"/api/categories/{Data.UpcomingUnder20CategoryId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_DeletesCategoryWithEntries_Returns409()
    {
        await PostJson(await AsParticipant(), "/api/enrollments",
            new { eventId = Data.UpcomingEventId, categoryId = Data.Upcoming10kCategoryId });

        var response = await (await AsOrganiser()).DeleteAsync($"/api/categories/{Data.Upcoming10kCategoryId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Anyone_ListsCategoriesForEvent()
    {
        var categories = await ReadAsync<List<CategoryResponse>>(
            await Anonymous().GetAsync($"/api/events/{Data.UpcomingEventId}/categories"));

        Assert.Equal(["10km", "Under 20"], categories.Select(c => c.CategoryName));
    }

    [Fact]
    public async Task ListCategories_UnknownEvent_Returns404()
    {
        var response = await Anonymous().GetAsync("/api/events/9999/categories");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
