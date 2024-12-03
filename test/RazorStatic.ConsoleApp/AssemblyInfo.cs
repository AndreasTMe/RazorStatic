using RazorStatic.Attributes;

[assembly: DirectoriesSetup(Pages = "Pages", Content = "Content")]

[assembly: CollectionDefinition(Key = "blog", PageRoute = "Blog", ContentDirectory = "Blog", Extension = "*.md")]
[assembly: CollectionDefinition(Key = "product", PageRoute = "Product", ContentDirectory = "Product")]

[assembly: StaticContent(RootPath = "wwwroot/css", EntryFile = "home.css")]
[assembly: StaticContent(RootPath = "wwwroot/css", Extensions = [".css"], EntryFile = "blog.css")]
[assembly: StaticContent(RootPath = "wwwroot/js", Extensions = [".js"])]
[assembly: StaticContent(RootPath = "wwwroot/images", Extensions = [".jpg", ".png"])]