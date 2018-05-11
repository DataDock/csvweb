#region copyright
// DataDock.CsvWeb is free and open source software licensed under the MIT License
// -------------------------------------------------------------------------------
// 
// Copyright (c) 2018 The DataDock Project (http://datadock.io/)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is furnished
// to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
#endregion
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
