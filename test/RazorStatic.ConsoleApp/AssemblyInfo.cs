using RazorStatic.Attributes;

[assembly: DirectoriesSetup(Pages = "Pages", Content = "Content")]

[assembly: CollectionDefinition(PageRoute = "Blog", ContentDirectory = "Blog")]
[assembly: CollectionDefinition(PageRoute = "Product", ContentDirectory = "Product")]

[assembly: StaticContent(IncludePaths = ["wwwroot/css"], EntryFile = "home.css")]
[assembly: StaticContent(IncludePaths = ["wwwroot/css"], Extensions = [".css"], EntryFile = "blog.css")]
[assembly: StaticContent(IncludePaths = ["wwwroot/js"], Extensions = [".js"])]
[assembly: StaticContent(IncludePaths = ["wwwroot/images"], Extensions = [".jpg", ".png"])]