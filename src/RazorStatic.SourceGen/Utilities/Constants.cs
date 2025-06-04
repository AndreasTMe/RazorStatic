namespace RazorStatic.SourceGen.Utilities;

internal static class Constants
{
    public const string RazorStaticAbstractionsNamespace = "RazorStatic.Abstractions";
    public const string RazorStaticAttributesNamespace   = "RazorStatic.Attributes";
    public const string RazorStaticComponentsNamespace   = "RazorStatic.Components";
    public const string RazorStaticCoreNamespace         = "RazorStatic.Core";
    public const string RazorStaticGeneratedNamespace    = "RazorStatic.Generated";
    public const string RazorStaticUtilitiesNamespace    = "RazorStatic.Utilities";

    public static class Abstractions
    {
        public static class CollectionFileComponentBase
        {
            public const string Name = nameof(CollectionFileComponentBase);

            public static class Members
            {
                public const string ContentFilePath = nameof(ContentFilePath);
                public const string Content         = nameof(Content);
                public const string FrontMatter     = nameof(FrontMatter);
                public const string Slug            = nameof(Slug);
            }
        }

        public static class CollectionFileGroupComponentBase
        {
            public const string Name = nameof(CollectionFileGroupComponentBase);

            public static class Members
            {
                public const string Slug     = nameof(Slug);
                public const string GroupBy  = nameof(GroupBy);
                public const string Metadata = nameof(Metadata);
            }
        }

        public static class PageCollectionDefinition
        {
            public const string Name = "Abstract" + nameof(PageCollectionDefinition);

            public static class Members
            {
                public const string RootPath                   = nameof(RootPath);
                public const string RenderComponentsAsync      = nameof(RenderComponentsAsync);
                public const string RenderGroupComponentsAsync = nameof(RenderGroupComponentsAsync);

                public const string GetFileContentAsync = nameof(GetFileContentAsync);
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
                public const string ProjectRoot = nameof(ProjectRoot);
                public const string Pages       = nameof(Pages);
                public const string Content     = nameof(Content);
            }
        }

        public static class DirectoriesSetupForStaticContent
        {
            public const string Name = "I" + nameof(DirectoriesSetupForStaticContent);
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
                public const string Key              = nameof(Key);
                public const string PageRoute        = nameof(PageRoute);
                public const string ContentDirectory = nameof(ContentDirectory);
                public const string StorageType      = nameof(StorageType);
            }
        }

        public static class CollectionExtension
        {
            public const string Name = nameof(CollectionExtension);

            public static class Members
            {
                public const string Key       = nameof(Key);
                public const string PageRoute = nameof(PageRoute);
                public const string GroupBy   = nameof(GroupBy);
            }
        }

        public static class DirectoriesSetup
        {
            public const string Name = nameof(DirectoriesSetup);

            public static class Members
            {
                public const string Pages   = nameof(Pages);
                public const string Content = nameof(Content);
            }
        }

        public static class StaticContent
        {
            public const string Name = nameof(StaticContent);

            public static class Members
            {
                public const string RootPath   = nameof(RootPath);
                public const string Extensions = nameof(Extensions);
                public const string EntryFile  = nameof(EntryFile);
            }
        }
    }

    public static class Enums
    {
        public static class StorageType
        {
            public const string Name = nameof(StorageType);

            public static class Values
            {
                public const string Local     = nameof(Local);
                public const string AzureBlob = nameof(AzureBlob);
                public const string AwsS3     = nameof(AwsS3);
            }
        }
    }
}