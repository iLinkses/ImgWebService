using System.Collections.Generic;

namespace ImgWebService
{
    internal class JsonAnswer
    {
        internal class images
        {
            public string alt { get; set; }
            public string src { get; set; }
            public string size { get; set; }
        }
        internal class RootObject
        {
            public string host { get; set; }
            public List<images> images { get; set; }
        }
        //public class Root
        //{
        //    public List<RootObject> RootObject { get; set; }
        //}
    }
}
