using RazorStatic.Attributes;

[assembly: DirectoriesSetup(Pages = "Pages", Content = "Content", Static = "wwwroot")]

[assembly: CollectionDefinition(PageRoute = "Blog", ContentDirectory = "Blog")]
[assembly: CollectionDefinition(PageRoute = "Product", ContentDirectory = "Product")]

[assembly: StaticContent(IncludePaths = ["wwwroot/css"], Extensions = [".css"], EntryFile = "wwwroot/css/global.css")]
[assembly: StaticContent(IncludePaths = ["wwwroot/js"], Extensions = [".js"])]
[assembly: StaticContent(IncludePaths = ["wwwroot/images"], Extensions = [".jpg", ".png"])]