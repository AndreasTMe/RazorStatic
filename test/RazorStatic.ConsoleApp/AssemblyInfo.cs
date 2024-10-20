using RazorStatic.Shared.Attributes;

[assembly: DirectoriesSetup(Pages = "Pages", Content = "Content", Tailwind = "Styles", Static = "wwwroot")]
[assembly: TailwindConfig(RootFilePath = "tailwind.css")]
[assembly: CollectionDefinition(PageRoute = "Blog", ContentDirectory = "Blog")]
[assembly: CollectionDefinition(PageRoute = "Product", ContentDirectory = "Product")]