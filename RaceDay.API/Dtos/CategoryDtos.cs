using System.ComponentModel.DataAnnotations;
using RaceDay.API.Models;

namespace RaceDay.API.Dtos;

/// <summary>Fields an organiser supplies when adding or updating a category.</summary>
public class CategoryRequest
{
    /// <example>10km</example>
    [Required, StringLength(50, MinimumLength = 1)]
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>Age (e.g. Under 20, Senior) or Distance (e.g. 10km, 21km).</summary>
    [Required]
    public CategoryType? CategoryType { get; set; }
}

/// <summary>A category as returned by the API.</summary>
public class CategoryResponse
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public CategoryType CategoryType { get; set; }

    public static CategoryResponse From(Category c) => new()
    {
        CategoryId = c.CategoryId,
        EventId = c.EventId,
        CategoryName = c.CategoryName,
        CategoryType = c.CategoryType
    };
}
