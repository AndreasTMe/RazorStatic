using RazorStatic.Attributes;

[assembly: DirectoriesSetup(Pages = "Pages", Content = "Content")]

[assembly: CollectionDefinition(Key = "blog", PageRoute = "Blog", ContentDirectory = "Blog")]
[assembly: CollectionExtension(Key = "blog", PageRoute = "Blog/Categories", GroupBy = "category")]
[assembly: CollectionExtension(Key = "blog", PageRoute = "Blog/Tags", GroupBy = "tag")]

[assembly: CollectionDefinition(Key = "product", PageRoute = "Product", ContentDirectory = "Product")]

[assembly: StaticContent(RootPath = "wwwroot/css", EntryFile = "home.css")]
[assembly: StaticContent(RootPath = "wwwroot/css", Extensions = [".css"], EntryFile = "blog.css")]
[assembly: StaticContent(RootPath = "wwwroot/js", Extensions = [".js"])]
[assembly: StaticContent(RootPath = "wwwroot/images", Extensions = [".jpg", ".png"])]