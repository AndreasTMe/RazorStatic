using RazorStatic.Shared.Attributes;

namespace RazorStatic.ConsoleApp;

[TailwindConfig(StylesFilePath = @"\styles.css", OutputFilePath = @"\out\styles.css")]
internal sealed partial class TailwindBuilder;

[PagesStore]
internal sealed partial class UserDefinedPagesStore;

[CollectionDefinition(PageRoute = @"\Blog", ContentDirectory = @"\Blog")]
internal sealed partial class BlogCollectionDefinition;

[CollectionDefinition(PageRoute = @"\Product", ContentDirectory = @"\Product")]
internal sealed partial class ProductCollectionDefinition;