using System.ComponentModel.DataAnnotations;
using BenBowenBlog.Data;

namespace BenBowenBlog.Web.Admin;

public sealed class CategoryEditModel {
	[Required, MaxLength(Category.NameMaxLength)]
	public string Name { get; set; } = "";

	[Required(ErrorMessage = "Choose an icon.")]
	public string? Icon { get; set; }
}