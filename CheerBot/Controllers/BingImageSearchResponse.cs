namespace CheerBot.Controllers
{
    
        public class BingImageSearchResponse
        {
            public string _type { get; set; }
            public int totalEstimatedMatches { get; set; }
            public string readLink { get; set; }
            public string webSearchUrl { get; set; }
            public ImageResult[] value { get; set; }
        }

        public class ImageResult
        {
            public string name { get; set; }
            public string webSearchUrl { get; set; }
            public string thumbnailUrl { get; set; }
            public object datePublished { get; set; }
            public string contentUrl { get; set; }
            public string hostPageUrl { get; set; }
            public string contentSize { get; set; }
            public string encodingFormat { get; set; }
            public string hostPageDisplayUrl { get; set; }
            public int width { get; set; }
            public int height { get; set; }
            public string accentColor { get; set; }
        }



    }
