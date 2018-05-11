namespace DataDock.CsvWeb.Metadata
{
    public class InheritedPropertyContainer
    {
        private UriTemplate _aboutUrl;
        private DatatypeDescription _datatype;
        private string _default;
        private string _lang;
        private UriTemplate _propertyUrl;
        private UriTemplate _valueUrl;

        public InheritedPropertyContainer(InheritedPropertyContainer parentContainer)
        {
            Parent = parentContainer;
        }

        public InheritedPropertyContainer Parent { get; }

        public UriTemplate AboutUrl
        {
            get { return _aboutUrl ?? Parent?.AboutUrl; }
            set { _aboutUrl = value; }
        }

        public DatatypeDescription Datatype
        {
            get { return _datatype ?? Parent?.Datatype; }
            set { _datatype = value; }
        }

        public string Default
        {
            get { return _default ?? Parent?.Default; }
            set { _default = value; }
        }

        public string Lang
        {
            get { return _lang ?? Parent?.Lang; }
            set { _lang = value; }
        }

        public UriTemplate PropertyUrl
        {
            get { return _propertyUrl ?? Parent?.PropertyUrl; }
            set { _propertyUrl = value; }
        }

        public UriTemplate ValueUrl
        {
            get { return _valueUrl ?? Parent?.ValueUrl; }
            set { _valueUrl = value; }
        }
    }
}
