using RazorStatic.Shared.Attributes;

[assembly: DirectoriesSetup(Pages = "Pages", Content = "Content", Static = "wwwroot")]
[assembly: CollectionDefinition(PageRoute = "Blog", ContentDirectory = "Blog")]
[assembly: CollectionDefinition(PageRoute = "Product", ContentDirectory = "Product")]