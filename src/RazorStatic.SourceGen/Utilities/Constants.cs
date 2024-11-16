namespace RazorStatic.SourceGen.Utilities;

internal static class Constants
{
    public static class Namespaces
    {
        public const string RazorStatic = nameof(RazorStatic);
        // ReSharper disable once MemberHidesStaticFromOuterClass
        public const string Abstractions = nameof(Abstractions);
        public const string Components   = nameof(Components);
        public const string Core         = nameof(Core);
    }
    
    public static class Abstractions
    {
        public static class FileComponentBase
        {
            public const string Name = nameof(FileComponentBase);

            public static class Members
            {
                public const string PageFilePath = nameof(PageFilePath);
            }
        }

        public static class CollectionFileComponentBase
        {
            public const string Name = nameof(CollectionFileComponentBase);

            public static class Members
            {
                public const string ContentFilePath = nameof(ContentFilePath);
            }
        }
    }

    public static class Interfaces
    {
        public static class DirectoriesSetup
        {
            public const string Name = "I" + nameof(DirectoriesSetup);

            public static class Members
            {
                public const string Pages   = nameof(Pages);
                public const string Content = nameof(Content);
                public const string Static  = nameof(Static);
            }
        }

        public static class PagesStore
        {
            public const string Name = "I" + nameof(PagesStore);

            public static class Members
            {
                public const string GetPageType          = nameof(GetPageType);
                public const string RenderComponentAsync = nameof(RenderComponentAsync);
            }
        }

        public static class PageCollectionDefinition
        {
            public const string Name = "I" + nameof(PageCollectionDefinition);

            public static class Members
            {
                public const string RootPath              = nameof(RootPath);
                public const string RenderComponentsAsync = nameof(RenderComponentsAsync);
            }
        }

        public static class PageCollectionsStore
        {
            public const string Name = "I" + nameof(PageCollectionsStore);

            public static class Members
            {
                public const string TryGetCollection = nameof(TryGetCollection);
            }
        }
    }

    public static class Attributes
    {
        public static class CollectionDefinition
        {
            public const string Name = nameof(CollectionDefinition);

            public static class Members
            {
                public const string PageRoute        = nameof(PageRoute);
                public const string ContentDirectory = nameof(ContentDirectory);
            }
        }

        public static class DirectoriesSetup
        {
            public const string Name = nameof(DirectoriesSetup);

            public static class Members
            {
                public const string Pages   = nameof(Pages);
                public const string Content = nameof(Content);
                public const string Static  = nameof(Static);
            }
        }

        public static class StaticContent
        {
            public const string Name = nameof(StaticContent);

            public static class Members
            {
                public const string IncludePaths = nameof(IncludePaths);
                public const string Extensions   = nameof(Extensions);
                public const string EntryFile    = nameof(EntryFile);
            }
        }
    }
}