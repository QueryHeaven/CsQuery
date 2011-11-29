﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Dynamic;
using System.Reflection;
using System.IO;
using System.Web.Script.Serialization;
using Jtc.CsQuery.ExtensionMethods;
using Jtc.CsQuery.Utility;

namespace Jtc.CsQuery
{
    /// <summary>
    /// Relationship between Dom and Selections
    /// 
    /// Dom represents a "DOM" and is shared among related CsQuery objects. This is the actual heirarchy. When a subquery is run, it 
    /// uses the same DOM as its parent. 
    /// 
    /// Selections is a set of elements matching a selector. Elements in a selector are typically a subset of DOM, though cloned
    /// elements will be unbound. Any operations which affect a DOM (versus a selection set) will apply to the DOM bound to this
    /// CsqQuery object.
    /// 
    /// DomRoot is a reference to the original CsQuery object that a DOM was created. 
    /// 
    /// 
    /// The static Create() methods create new DOMs. To create a CsQuery object based on an existing dom, use new CsQuery() 
    /// (similar to jQuery() methods).
    /// </summary>
    
    public partial class CsQuery : IEnumerable<IDomObject>
    {
        // TODO:
        // Detach
        // Empty
        // NextAll
        // NextUntil
        // End
        // WrapAll
        // WrapInner
        // ParentsUntil
        // NextUntil
        // OffsetParent
        // PrevAll
        // PrevUntil
        // Prepend
        // PrependTo
        // Slice
        // jquery.Contains
        // jquery.Grep
        // + some selectors

        

        /// <summary>
        /// Add the previous set of elements on the stack to the current set.
        /// </summary>
        /// <returns></returns>
        public CsQuery AndSelf()
        {
            var csq = new CsQuery(this);
            if (CsQueryParent == null)
            {
                return csq;
            }
            else
            {
                csq.Selection.AddRange(CsQueryParent.Selection);
                return csq;
            }
        }

        public int Length
        {
            get
            {
                return Selection.Count;
            }
        }
        /// <summary>
        /// Return matched element. 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public IDomObject this[int index]
        {
            get {
                return Get(index);
            }
        }

        
        public CsQuery this[string selector]
        {
            get
            {
                return Select(selector);
            }
        }

        public CsQuery this[IDomObject element]
        {
            get
            {
                return Select(element);
            }
        }
        public CsQuery this[IEnumerable<IDomObject> element]
        {
            get
            {
                return Select(element);
            }
        }
        public IEnumerable<IDomObject> Get()
        {
            return Selection;
        }
        public IDomObject Get(int index)
        {
            int effectiveIndex = index < 0 ? Selection.Count+index-1 : index;

            if (effectiveIndex >= 0 && effectiveIndex < Selection.Count)
            {
                return Selection.ElementAt(effectiveIndex);
            }
            else
            {
                return null;
            }

        }

        /// <summary>
        /// Set the HTML contents of each element in the set of matched elements. 
        /// Any elements without InnerHtml are ignored.
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public CsQuery Html(string html)
        {
            IDomFragment newElements = new DomFragment(html.ToCharArray());
            DomElementFactory factory = new DomElementFactory(newElements);
            newElements.ChildNodes.AddRange(factory.CreateObjects());

            foreach (DomElement obj in Selection)
            {
                if (obj.InnerHtmlAllowed)
                {
                    obj.ChildNodes.Clear();
                    obj.ChildNodes.AddRange(newElements.ChildNodes);
                }
            }
            return this;
        }
        /// <summary>
        /// Get the HTML contents of the first element in the set of matched elements.
        /// </summary>
        /// <returns></returns>
        public string Html()
        {
            if (Length > 0)
            {
                return this[0].InnerHTML;
            }
            else
            {
                return String.Empty;
            }
        }
        public CsQuery Not(string selector)
        {
            CsQuery csq = new CsQuery(Selection);
            csq.Selection.ExceptWith(Select(selector));
            return csq;
        }
        public CsQuery Not(IDomObject element)
        {
            return Not(Objects.ToEnumerable(element));
        }
        public CsQuery Not(IEnumerable<IDomObject> elements)
        {
            CsQuery csq = new CsQuery(Selection);
            csq.Selection.ExceptWith(elements);
            return csq;
        }
        /// <summary>
        /// Reduce the set of matched elements to those that have a descendant that matches the selector or DOM element.
        /// </summary>
        /// <param name="selector"></param>
        /// <returns></returns>
        public CsQuery Has(string selector)
        {
            
            var csq = New();
            foreach (IDomObject obj in Selection)
            {
                if (obj.Csq().Find(selector).Length>0)
                {
                    csq.Selection.Add(obj);
                }
            }
            return csq;
        }
        public CsQuery Has(IDomObject element)
        {
            return Has(Objects.ToEnumerable(element));
        }
        public CsQuery Has(IEnumerable<IDomObject> elements)
        {
            var csq = New();
            foreach (IDomObject obj in Selection)
            {
                if (obj.Csq().Find(elements).Length>0)
                {
                    csq.Selection.Add(obj);
                }
            }
            return csq;
        }
        /// <summary>
        /// Set the content of each element in the set of matched elements to the specified text.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public CsQuery Text(string value)
        {
            foreach (IDomElement obj in Elements)
            {
                if (obj.InnerTextAllowed)
                {
                    obj.ChildNodes.Clear();
                    // Element types that cannot have HTML contents should not have the value encoded.
                    string textValue = obj.InnerHtmlAllowed ? System.Web.HttpUtility.HtmlEncode(value) : value;
                    DomText text = new DomText(textValue);
                    obj.ChildNodes.Add(text);
                }
            }
            return this;
        }
        public CsQuery Text(Func<object,object,object> func) {

            return this;
        }

        /// <summary>
        /// Get the combined text contents of each element in the set of matched elements, including their descendants.
        /// </summary>
        /// <returns></returns>
        public string Text()
        {
            StringBuilder sb = new StringBuilder();

            IDomObject lastElement = null;
            foreach (IDomObject obj in Selection)
            {
                // Add a space between noncontiguous elements in the selection
                if (lastElement != null && obj.Index > 0
                    && obj.PreviousSibling != lastElement)
                {
                    sb.Append(" ");
                }
                lastElement = obj;
                if (obj.NodeType == NodeType.TEXT_NODE)
                {
                    sb.Append(obj.NodeValue);
                }
                else
                {
                    Text(sb, obj.Csq().Contents());
                }
            }
            return sb.ToString();
        }
        /// <summary>
        /// Helper for public Text() function to act recursively
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="elements"></param>
        protected void Text(StringBuilder sb, IEnumerable<IDomObject> elements)
        {
            IDomObject lastElement = null;
            foreach (IDomObject obj in elements)
            {
                if (lastElement != null && obj.Index > 0
                   && obj.PreviousSibling != lastElement)
                {
                    sb.Append(" ");
                }
                lastElement = obj;
                switch (obj.NodeType)
                {
                    case NodeType.TEXT_NODE:
                    case NodeType.CDATA_SECTION_NODE:
                    case NodeType.COMMENT_NODE:
                        sb.Append(obj.NodeValue);
                        break;
                    case NodeType.ELEMENT_NODE:
                        Text(sb, obj.ChildNodes);
                        break;
                }
            }
        }
        /// <summary>
        /// Add elements to the set of matched elements from a selector or an HTML fragment. Returns a new jQuery object.
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public CsQuery Add(string selector)
        {
            return Add(Select(selector));
        }
        /// <summary>
        ///  Add elements to the set of matched elements. Returns a new jQuery object
        /// </summary>
        /// <param name="elements"></param>
        /// <returns></returns>
        public CsQuery Add(IDomObject element)
        {
            return Add(Objects.ToEnumerable(element));
        }
        public CsQuery Add(IEnumerable<IDomObject> elements)
        {
            CsQuery res = new CsQuery(this);
            res.AddSelectionRange(elements);
            return res;
        }

        /// <summary>
        /// Adds the specified class(es) to each of the set of matched elements.
        /// </summary>
        /// <param name="className"></param>
        /// <returns></returns>
        public CsQuery AddClass(string className)
        {
            foreach (var item in Elements)
            {
                item.AddClass(className);
            }
            return this;
        }
        /// <summary>
        /// Add or remove one or more classes from each element in the set of matched elements, 
        /// depending on either the class's presence.
        /// </summary>
        /// <param name="className"></param>
        /// <returns></returns>
        public CsQuery ToggleClass(string classes)
        {
            IEnumerable<string> classList = classes.SplitClean(' ');
            foreach (IDomElement el in Elements) {
                foreach (string cls in classList)
                {
                    if (el.HasClass(cls))
                    {
                        el.RemoveClass(cls);
                    }
                    else
                    {
                        el.AddClass(cls);
                    }
                }
            }
            return this;
        }
        /// <summary>
        /// Add or remove one or more classes from each element in the set of matched elements, 
        /// depending on the value of the switch argument.
        /// </summary>
        /// <param name="className"></param>
        /// <returns></returns>
        public CsQuery ToggleClass(string classes, bool addRemoveSwitch)
        {
            IEnumerable<string> classList = classes.SplitClean(' ');
            foreach (IDomElement el in Elements)
            {
                foreach (string cls in classList)
                {
                    if (addRemoveSwitch)
                    {
                        el.AddClass(cls); 
                    }
                    else
                    {
                        el.RemoveClass(cls);
                    }
                }
            }
            return this;
        }
        /// <summary>
        /// Determine whether any of the matched elements are assigned the given class.
        /// </summary>
        /// <param name="className"></param>
        /// <returns></returns>
        public bool HasClass(string className)
        {
            
            IDomElement el = FirstElement();

            return el==null ? false :
                el.HasClass(className);
        }
        /// <summary>
        /// Insert content, specified by the parameter, to the end of each element in the set of matched elements.
        /// TODO: Add overloads with multiple values
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public CsQuery Append(params string[] content)
        {
            IDomFragment fragment = new DomFragment();
            DomElementFactory factory = new DomElementFactory(fragment);
            foreach (var item in content)
            {
                IEnumerable<IDomObject> els = factory.CreateObjects(item);
                Append(els);
            }
            return this;
        }
        public CsQuery Append(IDomObject element)
        {
            return Append(Objects.ToEnumerable(element));
        }
        public CsQuery Append(IEnumerable<IDomObject> elements)
        {
            bool first = true;
            foreach (var obj in Elements )
            {
                // must copy the enumerable first, since this can cause
                // els to be removed from it
                List<IDomObject> list = new List<IDomObject>(elements);
                foreach (var e in list)
                {
                    obj.AppendChild(first ? e : e.Clone());
                }
                first = false;
            }
            return this;
        }
        /// <summary>
        ///  Insert every element in the set of matched elements to the end of the target.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public CsQuery AppendTo(string target)
        {
            return AppendTo(Select(target));

        }
        public CsQuery AppendTo(IEnumerable<IDomObject> target)
        {
            CsQuery outputSet = New();
            bool first = true;
            foreach (IDomObject e in target)
            {
                if (e is IDomContainer) {
                    IEnumerable<IDomObject> source;
                    if (first) {
                        source = this;
                        first=false;
                    } else {
                        source=Clone();
                    }
                    
                    foreach (IDomObject obj in source)
                    {
                        e.AppendChild(obj);
                    }
                    outputSet.AddSelectionRange(source);
                }
            }
            return outputSet;
        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="func">
        /// delegate(int index, string html) 
        ///  A function that returns an HTML string to insert at the end of each element in the set of matched elements. 
        /// Receives the index position of the element in the set and the old HTML value of the element as arguments.
        /// </param>
        /// <returns></returns>
        public CsQuery Append(Func<int, string, string> func)
        {
            int index = 0;
            foreach (DomElement obj in Elements)
            {

                string val = func(index, obj.InnerHTML);
                obj.Csq().Append((string)val);
                index++;
            }
            return this;
        }
        public CsQuery Append(Func<int, string, IDomElement> func)
        {
            int index = 0;
            foreach (IDomElement obj in Elements)
            {
                IDomElement clientValue = func(index, obj.InnerHTML);
                obj.Csq().Append(clientValue);
                index++;
            }
            return this;
        }
        public CsQuery Append(Func<int, string, IEnumerable<IDomElement>> func)
        {
            int index = 0;
            foreach (DomElement obj in Elements)
            {
                IEnumerable<IDomElement> val = func(index, obj.InnerHTML);
                obj.Csq().Append(val);
                index++;
            }
            return this;
        }
        /// <summary>
        /// Get the value of an attribute for the first element in the set of matched elements.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string Attr(string name)
        {
            name= name.ToLower();
            if (Length > 0)
            {
                string value;
                var el = this[0];
                switch(name) { 
                    case "class":
                        return el.ClassName;
                    case "style":
                        string st=  el.Style.ToString();
                        return st == "" ? null : st;
                    default:
                        if (el.TryGetAttribute(name, out value))
                        {
                            if (DomData.IsBoolean(name))
                            {
                                // Pre-1.6 and 1.6.1+ compatibility: always return the name of the attribute if it exists for
                                // boolean attributes
                                return name;
                            }
                            else
                            {
     
                                return value; 
                            }
                        } else if (name=="value" &&
                            (el.NodeName =="input" || el.NodeName=="select" || el.NodeName=="option")) {
                            return Val();
                        } else if (name=="value" && el.NodeName =="textarea") {
                            return el.InnerText;
                        }
                        break;
                }
                
            }
            return null;
        }
        
        /// <summary>
        /// Set one or more attributes for the set of matched elements.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public CsQuery Attr(string name, object value)
        {
            bool isBoolean = DomData.IsBoolean(name);
            if (isBoolean)
            {
                // Using attr with empty string should set a property to "true. But prop() itself requires a truthy value. Check for this specifically.
                if (value is string && (string)value == String.Empty)
                {
                    value = true;
                }
                SetProp(name, value);
                return this;
            }

            string val;
            if (value is bool)
            {
                val = value.ToString().ToLower();
            }
            else
            {
                val = GetValueString(value);
            }

            foreach (DomElement e in Elements)
            {
                if ((e.NodeName == "input" || e.NodeName == "button") && name == "type"
                    && !e.IsDisconnected)
                {
                    throw new Exception("Can't change type of input elements in DOM");
                }
                e.SetAttribute(name, val);
            }
            return this;
        }
        /// <summary>
        /// Set attributes from a JSON string
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public CsQuery AttrSet(string json)
        {
            object parsed = ParseJSON(json);
            if (!(parsed is IDictionary<string, object>))
            {
                throw new Exception("Cannot parse string into name/value pairs");
            }
            IDictionary<string, object> dict = (IDictionary<string, object>)parsed;
            return Attr(dict);
        }
        /// <summary>
        /// Sets the attributes of the selected elements from an object of key/value pairs
        /// </summary>
        /// <param name="attributes"></param>
        /// <returns></returns>
        public CsQuery Attr(IDictionary<string,object> attributes) 
        {

            foreach (IDomElement el in Elements)
            {
                foreach (var kvp in attributes)
                {
                    string name = kvp.Key.ToLower();
                    switch(name) {
                        case "css":
                            Select(el).Css((IDictionary<string,object>)kvp.Value);
                            break;
                        case "html":
                            Select(el).Html(kvp.Value.ToString());
                            break;
                        case "height":
                        case "width":
                            // for height and width, do not set attributes - set css
                            Select(el).Css(name, kvp.Value.ToString());
                            break;
                        case "text":
                            Select(el).Text(kvp.Value.ToString());
                            break;
                        default:
                            el.SetAttribute(kvp.Key, kvp.Value.ToString());
                            break;
                    }
                }
            }
            return this;
        }
        /// <summary>
        /// Perform a substring replace on the contents of the named attribute in each item in the selection set. 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="replaceWhat"></param>
        /// <param name="replaceWith"></param>
        /// <returns></returns>
        public CsQuery AttrReplace(string name, string replaceWhat, string replaceWith)
        {
            foreach (IDomElement item in Selection)
            {
                string val = item[name];
                if (val != null)
                {
                    item[name] = val.Replace(replaceWhat, replaceWith);
                }
            }
            return this;
        }
        /// <summary>
        /// Remove an attribute from each element in the set of matched elements.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public CsQuery RemoveAttr(string name)
        {
            foreach (DomElement e in Elements)
            {
                switch (name)
                {
                    case "class":
                        e.ClassName = "";
                        break;
                    case "style":
                        e.Style.Clear();
                        break;
                    default:
                        e.RemoveAttribute(name);
                        break;
                }
            }
            return this;
        }
        /// <summary>
        ///  Remove a property for the set of matched elements.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public CsQuery RemoveProp(string name)
        {
            return RemoveAttr(name);
        }
        /// <summary>
        /// Insert content, specified by the parameter, before each element in the set of matched elements.
        /// </summary>
        /// <param name="?"></param>
        /// <returns></returns>
        public CsQuery Before(string selector)
        {
            return Before(Select(selector));
        }
        public CsQuery Before(IDomObject element)
        {
            return Before(Objects.ToEnumerable(element));
        }
        /// <summary>
        /// Insert content, specified by the parameter, before each element in the set of matched elements.
        /// </summary>
        public CsQuery Before(IEnumerable<IDomObject> selection)
        {
            var source = new CsQuery(selection, this);
            source.InsertBefore(Selection);
            return this;
        }
        /// <summary>
        ///  Insert content, specified by the parameter, after each element in the set of matched elements.
        /// </summary>
        /// <param name="selector"></param>
        /// <returns></returns>
        public CsQuery After(string selector)
        {
            return After(Select(selector));
        }
        public CsQuery After(IDomObject element)
        {
            return After(Objects.ToEnumerable(element));
        }
        public CsQuery After(IEnumerable<IDomObject> selection)
        {
            var source = new CsQuery(selection,this);
            source.InsertAfter(Selection);
            return this;
        }

        /// <summary>
        /// Remove the parents of the set of matched elements from the DOM, 
        /// leaving the matched elements in their place.
        /// </summary>
        /// <returns></returns>
        public CsQuery Unwrap()
        {
            HashSet<IDomObject> parents = new HashSet<IDomObject>();
            
            // Start with a unique list of parents instead of working with the siblings
            // to avoid repetition and unwrapping more than once for multiple siblings from
            // a single parent
            foreach (IDomObject obj in Selection)
            {
                if (obj.ParentNode != null) {
                    parents.Add(obj.ParentNode);
                }
            }
            foreach (IDomObject obj in parents) {
                var csq = obj.Csq();
                csq.ReplaceWith(csq.Contents());

            }
            return this;
        }
        public CsQuery Wrap(string wrappingSelector)
        {
            return Wrap(Select(wrappingSelector));
        }
        public CsQuery Wrap(IDomObject element)
        {
            return Wrap(Objects.ToEnumerable(element));
        }
        public CsQuery Wrap(IEnumerable<IDomObject> wrapper)
        {
            // get innermost structure
            CsQuery wrapperTemplate = EnsureInWrapper(wrapper);
            IDomElement wrappingEl= null;
            IDomElement wrappingElRoot=null;

            int depth = getInnermostContainer(wrapperTemplate.Elements, out wrappingEl, out wrappingElRoot);
          
            if (wrappingEl!=null) {
                IDomObject nextEl = null;
                IDomElement innerEl = null;
                IDomElement innerElRoot = null;
                foreach (IDomObject el in Selection)
                {

                    if (nextEl==null 
                        || !ReferenceEquals(nextEl,el))
                    {
                        var template = wrappingElRoot.Csq().Clone();
                        if (el.ParentNode != null)
                        {
                            template.InsertBefore(el);
                        } 
                        // This will always succceed because we tested before this loop. But we need
                        // to run it again b/c it's a clone now
                        getInnermostContainer(template.Elements, out innerEl, out innerElRoot);
                    }
                    nextEl = el.NextSibling;
                    innerEl.AppendChild(el);
                    
                }
            }
            return this;
        }
       
        //protected 
        /// <summary>
        /// Get the children of each element in the set of matched elements, optionally filtered by a selector.
        /// </summary>
        /// <returns></returns>
        public CsQuery Children(string selector=null)
        {
            return FilterIfSelector(SelectionChildren(), selector);
        }
        /// <summary>
        /// Description: Get the siblings of each element in the set of matched elements, optionally filtered by a selector.
        /// </summary>
        /// <returns></returns>
        public CsQuery Siblings(string selector=null)
        {
            SelectionSet<IDomElement> siblings = new SelectionSet<IDomElement>();
            
            // Add siblings of each item in the selection except the item itself for that iteration.
            // If two siblings are in the selection set, then all children of their mutual parent should
            // be returned. Otherwise, all children except the item iteself.
            foreach (var item in Selection)
            {
                foreach (var child in item.ParentNode.ChildElements) {
                    if (!ReferenceEquals(child,item))
                    {
                        siblings.Add(child);
                    }
                }
            }
            return FilterIfSelector(siblings, selector);
        }
        /// <summary>
        /// Create a deep copy of the set of matched elements.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public CsQuery Clone()
        {
            CsQuery csq = new CsQuery();
            
            foreach (IDomObject elm in Selection)
            {
                IDomObject clone = elm.Clone();
                csq.Document.AppendChild(clone);
                csq.AddSelection(clone);
            }
            return csq;
        }
        /// <summary>
        /// Get the first ancestor element that matches the selector, beginning at the current element and progressing up through the DOM tree.
        /// </summary>
        /// <returns></returns>
        public CsQuery Closest(string selector)
        {
            CsQuery matchTo = Select(selector);
            return Closest(matchTo);
        }
        public CsQuery Closest(IDomObject element)
        {
            return Closest(Objects.ToEnumerable(element));
        }
        public CsQuery Closest(IEnumerable<IDomObject> elements)
        {
            // Use a hashset to operate faster - since we already haveone for the selection set anyway
            SelectionSet<IDomObject> selectionSet;
            if (elements is CsQuery)
            {
                selectionSet = ((CsQuery)elements).Selection;
            }
            else
            {
                selectionSet = new SelectionSet<IDomObject>();
                selectionSet.AddRange(elements);
            }
            CsQuery csq = New();

            foreach (var el in Selection)
            {
                var search = el;
                while (search != null)
                {
                    if (selectionSet.Contains(search))
                    {
                        csq.AddSelection(search);
                        return csq;
                    }
                    search = search.ParentNode;
                }

            }
            return csq;

        }
        /// <summary>
        /// Get the children of each element in the set of matched elements, including text and comment nodes.
        /// </summary>
        /// <returns></returns>
        public CsQuery Contents()
        {

            List<IDomObject> list = new List<IDomObject>();
            foreach (IDomObject obj in Selection)
            {
                if (obj is IDomContainer)
                {
                    list.AddRange(obj.ChildNodes );
                }
            }

            return new CsQuery(list, this);
        }
        /// <summary>
        ///  Set one or more CSS properties for the set of matched elements from JSON data
        /// </summary>
        /// <param name="cssJson"></param>
        /// <returns></returns>
        public CsQuery CssSet(string json)
        {
            object parsed = ParseJSON(json);
            if (!(parsed is IDictionary<string,object>)) {
                throw new Exception("Cannot parse sting into name/value pairs");
            }
            IDictionary<string, object> dict = (IDictionary<string, object>)parsed;
            return Css(dict);
        }
        public CsQuery Css(IDictionary<string,object> css)
        {
            return this.Each((IDomElement e) =>
            {
                foreach (var key in css)
                {
                    e.Style[key.Key]= key.Value.ToString();
                }
            });
        }
        /// <summary>
        ///  Set one or more CSS properties for the set of matched elements.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public CsQuery Css(string name, string value)
        {
            string style = String.Empty;

            foreach (IDomElement e in Elements)
            {
                e.Style[name]=value;
            }
            return this;
        }

        /// <summary>
        /// Get the value of a style property for the first element in the set of matched elements

        /// </summary>
        /// <param name="style"></param>
        /// <returns></returns>
        public string Css(string style)
        {
            if (Length == 0)
            {
                return null;
            }
            else
            {
                return ((IDomElement)this[0]).Style[style];
            }
        }

        /// <summary>
        /// Store arbitrary data associated with the specified element. Returns the value that was set.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="jsonData"></param>
        /// <returns></returns>
        public CsQuery Data(string key,string jsonData)
        {
            this.Each((IDomElement e) =>
            {
                e.SetAttribute("data-" + key, jsonData);
            });
            return this;
        }
        /// <summary>
        /// Convert an object to JSON and store as data
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public CsQuery Data(string key, object data)
        {
            string json = CsQuery.ToJSON(data);
            this.Each((IDomElement e) =>
            {
                e.SetAttribute("data-" + key, json);
            });
            return this;
        }
        /// <summary>
        /// Returns value at named data store for the first element in the jQuery collection, as set by data(name, value).
        /// </summary>
        public object Data(string element)
        {
            string data = First().Attr("data-" + element);
            
            return CsQuery.ParseJSON(data);
            
        }
        public T Data<T>(string key)
        {
            string data = First().Attr("data-" + key);
            return CsQuery.ParseJSON<T>(data);
        }
        /// <summary>
        /// Returns data as a string, with no attempt to decode it
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string DataRaw(string key)
        {
            return First().Attr("data-" + key);
        }
        /// <summary>
        /// Iterate over each matched element.
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public CsQuery Each(Action<int, IDomElement> func)
        {
            int index = 0;
            foreach (IDomElement obj in Elements)
            {
                func(index, obj);
            }
            return this;
        }

        /// <summary>
        /// Iterate over each matched element.
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>

        public CsQuery Each(Action<IDomElement> func)
        {
            foreach (IDomElement obj in Elements)
            {
                func(obj);
            }
            return this;
        }
        /// <summary>
        /// Reduce the set of matched elements to the one at the specified index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public CsQuery Eq(int index)
        {
            if (index < 0)
            {
                index = Length + index-1;
            }
            if (index >= 0 && index < Length)
            {
                return new CsQuery(this[index], this);
            }
            else
            {
                return New();
            }
        }

        
        /// <summary>
        /// Get the descendants of each element in the current set of matched elements, filtered by a selector, jQuery object, or element.
        /// </summary>
        /// <param name="selector"></param>
        /// <returns></returns>
        public CsQuery Find(string selector)
        {
            CsQuery csq = New();
            CsQuerySelectors selectors = new CsQuerySelectors(selector);
            csq.AddSelectionRange(selectors.Select(Document, Children()));
            return csq;
        }
        public CsQuery Find(IEnumerable<IDomObject> elements)
        {
            CsQuery csq = New();
            CsQuerySelectors selectors = new CsQuerySelectors(elements);
            csq.AddSelectionRange(selectors.Select(Document, Children()));
            return csq;
        }
        public CsQuery Find(IDomObject element)
        {
            CsQuery csq =New();
            CsQuerySelectors selectors = new CsQuerySelectors(element);
            csq.AddSelectionRange(selectors.Select(Document, Children()));
            return csq;
        }

        public CsQuery Filter(string selector)
        {
            return new CsQuery(_FilterElements(Selection, selector));

        }
        public CsQuery Filter(IDomObject element) {
            return Filter(Objects.ToEnumerable(element));
        }
        public CsQuery Filter(IEnumerable<IDomObject> elements) {
            CsQuery filtered = new CsQuery(this);
            filtered.Selection.IntersectWith(elements);
            return filtered;            
        }
        public CsQuery Filter(Func<IDomObject, bool> function)
        {
            CsQuery result = New();
            foreach (IDomObject obj in Selection)
            {
                if (function(obj)) {
                    result.AddSelection(obj);
                }
            }
            return result;
        }
        public CsQuery Filter(Func<IDomObject, int, bool> function)
        {
            CsQuery result = New();
            int index = 0;
            foreach (IDomObject obj in Selection)
            {
                if (function(obj,index++))
                {
                    result.AddSelection(obj);
                }
            }
            return result;
        }

        /// <summary>
        /// Select elements and return a new CSQuery object 
        /// </summary>
        /// <param name="selector"></param>
        /// <returns></returns>
        public CsQuery Select(string selector)
        {
            CsQuerySelectors selectors = new CsQuerySelectors(selector);
           
            CsQuery csq = New();
            // If the selector is HTML create it as a new fragment so it can be indexed & traversed upon
            //IDomRoot dom = selectors.IsHtml ? new DomFragment(selector.ToCharArray()) : Document;
            csq.AddSelectionRange(selectors.Select(Document));
            return csq;
        }

        public CsQuery Select(string selector, CsQuery context)
        {
            return new CsQuery(selector, context);
        }

        public CsQuery Select(IDomObject element)
        {
            CsQuery csq = new CsQuery(element,this);
            return csq;
        }
        public CsQuery Select(IEnumerable<IDomObject> elements)
        {
            CsQuery csq = new CsQuery(elements,this);
            return csq;
        }

        /// <summary>
        /// Reduce the set of matched elements to the first in the set.
        /// </summary>
        /// <returns></returns>
        public CsQuery First()
        {
            return Eq(0);
        }
        /// <summary>
        /// Reduce the set of matched elements to the last in the set.
        /// </summary>
        /// <returns></returns>
        public CsQuery Last()
        {
            if (Selection.Count == 0)
            {
                return New();
            }
            else
            {
                return Eq(Selection.Count - 1);
            }
        }
        /// <summary>
        /// Hide the matched elements.
        /// </summary>
        /// <returns></returns>
        public CsQuery Hide()
        {
            return this.Each((IDomElement e) =>
            {
                e.Style["display"]= "none";
            });
        }
        /// <summary>
        /// Toggle the visiblity state of the matched elements.
        /// </summary>
        /// <returns></returns>
        public CsQuery Toggle()
        {
            return this.Each((IDomElement e) =>
            {
                string displ = e.Style["display"];
                bool isVisible = displ == null || displ != "none";
                e.Style["display"] = isVisible ? "none" : null;
            });
        }
        /// <summary>
        /// Display or hide the matched elements.
        /// </summary>
        /// <returns></returns>
        public CsQuery Toggle(bool isVisible)
        {
            return this.Each((IDomElement e) =>
            {
                if (isVisible)
                {
                    e.RemoveStyle("display");
                }
                else
                {
                    e.Style["display"] = "none";
                }
            });
        }
        /// <summary>
        /// Search for a given element from among the matched elements.
        /// </summary>
        /// <returns></returns>
        public int Index()
        {
            IDomObject el = Selection.FirstOrDefault();
            if (el != null)
            {
                return GetElementIndex(el);
            }
            return -1;
        }
        /// <summary>
        /// Returns the position of the current selection within the new selection defined by "selector"
        /// </summary>
        /// <param name="selector"></param>
        /// <returns></returns>
        public int Index(string selector)
        {
            var selection = Select(selector);
            return selection.Index(Selection);
        }
        public int Index(IDomObject elements)
        {
            return Index(Objects.ToEnumerable(elements));
        }
        public int Index(IEnumerable<IDomObject> elements)
        {
            IDomObject find = elements.FirstOrDefault();
            int index = -1;
            if (find != null)
            {
                int count = 0;
                foreach (IDomObject el in Selection)
                {
                    if (ReferenceEquals(el, find))
                    {
                        index=count;
                        break;
                    }
                    count++;
                }
            }
            return index;
        }

        /// <summary>
        /// Insert every element in the set of matched elements after the target.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public CsQuery InsertAfter(IDomObject target)
        {
            return InsertAtOffset(target,1);
        }
        public CsQuery InsertAfter(IEnumerable<IDomObject> target) {
            return InsertAtOffset(target, 1);
        }
        public CsQuery InsertAfter(string target)
        {
            return InsertAfter(Select(target));
        }
        /// <summary>
        /// Support for InsertAfter and InsertBefore. An offset of 0 will insert before the current element. 1 after.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        protected CsQuery InsertAtOffset(IDomObject target, int offset)
        {
            int index = target.Index;
            
            foreach (var item in Selection)
            {
                target.ParentNode.ChildNodes.Insert(index+offset,item);
                index++;
            }
            return this;
        }

        /// <summary>
        /// A selector, element, HTML string, or jQuery object; the matched set of elements will be inserted before the element(s) specified by this parameter.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public CsQuery InsertBefore(IDomObject target)
        {
            return InsertAtOffset(target, 0);
        }
        public CsQuery InsertBefore(IEnumerable<IDomObject> target)
        {
            return InsertAtOffset(target, 0);
        }

        /// <summary>
        /// Get the immediately following sibling of each element in the set of matched elements. 
        /// If a selector is provided, it retrieves the next sibling only if it matches that selector.
        /// </summary>
        /// <param name="selector"></param>
        /// <returns></returns>
        public CsQuery Next(string selector=null)
        {
            return FilterIfSelector(AdjacentElements(true),selector);
        }

        /// <summary>
        /// Get the parent of each element in the current set of matched elements, optionally filtered by a selector.
        /// </summary>
        /// <param name="selector"></param>
        /// <returns></returns>
        public CsQuery Parent(string selector=null)
        {
            SelectionSet<IDomObject> list = new SelectionSet<IDomObject>();

            foreach (IDomElement obj in Elements)
            {
                if (obj.ParentNode is IDomElement)
                {
                    list.Add((IDomElement)obj.ParentNode);
                }
            }
            return FilterIfSelector(list, selector);
        }
        /// <summary>
        ///  Get the ancestors of each element in the current set of matched elements, optionally filtered by a selector.
        /// </summary>
        /// <returns></returns>
        public CsQuery Parents(string selector=null)
        {
            CsQuery csq = New();
            csq.Selection.IsSorted = false;

            foreach (IDomElement obj in Elements)
            {
                if (obj.ParentNode is IDomElement)
                {
                    csq.Selection.Add((IDomElement)obj.ParentNode);
                    csq.Selection.AddRange(obj.ParentNode.Csq().Parents());
                }
            }
            if (selector == null)
            {
                return csq;
            } else {
                csq.Selection.IntersectWith(_FilterElements(csq.Selection,selector));
                return csq;
            }
        }
        /// <summary>
        /// Get the immediately preceding sibling of each element in the set of matched elements, optionally filtered by a selector.
        /// </summary>
        /// <returns></returns>
        public CsQuery Prev()
        {
            return Prev(null);
        }
        /// <summary>
        /// Set one or more properties for the set of matched elements.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public CsQuery Prop(string name, object value)
        {
            // Prop actually works on things other than boolean - e.g. SelectedIndex. For now though only use prop for booleans

            if (DomData.IsBoolean(name))
            {
                SetProp(name, value);
            }
            else
            {
                Attr(name, value);
            }
            return this;
        }
        public bool Prop(string name)
        {
            name=name.ToLower();
            if (Length>0 && DomData.IsBoolean(name)) {
                bool has = this[0].HasAttribute(name);
                // if there is nothing with the "selected" attribute, in non-multiple select lists, 
                // the first one is selected by default by Sizzle. We will return that same information 
                // when using prop.
                // TODO: this won't work for the "selected" selector. Need to move this logic into DomElement 
                // and use selected property instead.
                if (name == "selected" && !has)
                {
                    var owner = First().Closest("select");
                    string ownerSelected = owner.Val();
                    if (ownerSelected == String.Empty && !owner.Prop("multiple"))
                    {
                        return ReferenceEquals(owner.Find("option")[0], this[0]);
                    }

                }
                return has;
            }
            return false;
        }

        public CsQuery Prev(string selector)
        {
            return FilterIfSelector(AdjacentElements(false), selector);
        }   

        /// <summary>
        /// Remove all selected elements from the DOM
        /// </summary>
        /// <returns></returns>
        public CsQuery Remove(string selector=null)
        {
            IEnumerable<IDomObject> list = !String.IsNullOrEmpty(selector) ?
                Filter(selector).Selection :
                Selection;

            foreach (var el in list)
            {
                el.Remove();
            }
            return this;
        }
        
        /// <summary>
        /// Remove a single class, multiple classes, or all classes from each element in the set of matched elements.
        /// </summary>
        /// <param name="className"></param>
        /// <returns></returns>
        public CsQuery RemoveClass(string className=null)
        {
            
            return this.Each((IDomElement e) =>
            {
                if (!String.IsNullOrEmpty(className))
                {
                    e.RemoveClass(className);
                }
                else
                {
                    e.ClassName = null;
                }
            });
        }

        /// <summary>
        /// Remove a previously-stored piece of data.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public CsQuery RemoveData(string dataId=null)
        {
            foreach (IDomElement el in Elements)
            {
                List<string> toRemove = new List<string>();
                foreach (var kvp in el.Attributes)
                {
                    bool match = String.IsNullOrEmpty(dataId) ?
                        kvp.Key.StartsWith("data-") :
                        kvp.Key == "data-" + dataId;
                    if (match) 
                    {
                        toRemove.Add(kvp.Key);
                    }
                }
                foreach (string key in toRemove)
                {
                    el.Attributes.Remove(key);
                }
            }
            return this;
        }
        /// <summary>
        /// Determine whether an element has any jQuery data associated with it.
        /// </summary>
        /// <returns></returns>
        public bool HasData()
        {
            foreach (IDomElement el in Elements)
            {
                foreach (var kvp in el.Attributes)
                {
                    if (kvp.Key.StartsWith("data-"))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Replace each element in the set of matched elements with the provided new content.
        /// </summary>
        /// <param name="selector"></param>
        /// <returns></returns>
        public CsQuery ReplaceWith(string selector)
        {
            CsQuery newContent = new CsQuery(selector, this);
            return Before(newContent).Remove();
        }
        public CsQuery ReplaceWith(IEnumerable<IDomObject> selection)
        {
            return Before(selection).Remove();
        }
        public CsQuery Show()
        {
            foreach (IDomElement e in Elements)
            {
                e.RemoveStyle("display");
            }
            return this;
        }
        // Not used yet, will be for visible selector
        // Also not correct
        //protected bool IsVisible()
        //{
        //    bool parentHidden = false;
        //    IDomObject el = e.ParentNode;
        //    while (el != null)
        //    {
        //        string st = el.Style["display"];
        //        if (st == "none")
        //        {
        //            parentHidden = true;
        //            break;
        //        }
        //        el = el.ParentNode;
        //    }
        //    return parentHidden;
        //}
        /// <summary>
        /// Get the current value of the first element in the set of matched elements, and try to convert to the specified type
        /// </summary>
        /// <returns></returns>
        public T Val<T>()
        {
            string val = Val();
            return Objects.Convert<T>(val);
        }
        /// <summary>
        /// Get the current value of the first element in the set of matched elements.
        /// </summary>
        /// <returns></returns>
        public string Val()
        {
            if (Length > 0)
            {
                IDomElement e = this.Elements.First();
                switch(e.NodeName) {
                    case "textarea":
                        return e.InnerText;
                    case "input":
                        string val = e.GetAttribute("value",String.Empty);
                        switch(e.GetAttribute("type",String.Empty)) {
                            case "radio":
                            case "checkbox":
                                if (String.IsNullOrEmpty(val))
                                {
                                    val = "on";
                                }
                                break;
                            default:
                                break;
                        }
                        return val;
                    case "select":
                        string result = String.Empty;
                        // TODO optgroup handling (just like the setter code)
                        var options =Find("option");
                        if (options.Length==0) {
                            return null;
                        }
                        
                        foreach (IDomElement child in options)
                        {
                            bool disabled = child.HasAttribute("disabled") || (child.ParentNode.NodeName == "optgroup" && child.ParentNode.HasAttribute("disabled"));

                            if (child.HasAttribute("selected") && !disabled)
                            {
                                result = result.ListAdd(child.GetAttribute("value", String.Empty), ",");
                                if (!e.HasAttribute("multiple"))
                                {
                                    break;
                                }
                            }
                        }
                        
                        if (result == String.Empty)
                        {
                            result = options[0].GetAttribute("value", String.Empty);
                        }
                        return result;
                    case "option":
                        val = e.GetAttribute("value");
                        return val ?? e.InnerText;
                    default:
                        return e.GetAttribute("value",String.Empty);
                }
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// Set the value of each element in the set of matched elements. If a comma-separated value is passed to a multuple select list, then it
        /// will be treated as an array.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public CsQuery Val(object value)
        {
            bool first = true;
            string val = GetValueString(value);
            foreach (IDomElement e in Elements)
            {
                switch (e.NodeName)
                {
                    case "textarea":
                        // should we delete existing children first? they should not exist
                        e.InnerText = val;
                        break;
                    case "input":
                        switch (e.GetAttribute("type",String.Empty))
                        {
                            case "checkbox":
                            case "radio":
                                if (first)
                                {
                                    SetOptionSelected(Elements, value, true);
                                }
                                break;
                            default:
                                e.SetAttribute("value", val);
                                break;
                        }
                        break;
                    case "select":
                        if (first) {
                            var multiple = e.HasAttribute("multiple");
                            SetOptionSelected(e.ChildElements, value, multiple);
                        }
                        break;
                    default:
                        e.SetAttribute("value", val);
                        break;
                }
                first = false;

            }
            return this;
        }
 
        

        /// <summary>
        /// Set the value of each mutiple select element in the set of matched elements. Any elements not of type &lt;SELECT multiple&gt;&lt;/SELECT&gt; will be ignored.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        //public CsQuery Val(IEnumerable<object> values) {
        //    string valueString=String.Empty;
        //    foreach (object val in values) {
        //        valueString+=(String.IsNullOrEmpty(val.ToString())?String.Empty:",") + val.ToString();
        //    }
        //    foreach (IDomElement e in Elements)
        //    {
        //        if (e.NodeName == "select" && e.HasAttribute("multiple"))
        //        {
        //            Val(valueString);
        //        }
        //    }
        //    return this;
        //}
        /// <summary>
        /// Set the CSS width of each element in the set of matched elements.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public CsQuery Width(int value)
        {
            return Width(value.ToString() + "px");
        }
        public CsQuery Width(string value)
        {
            return Css("width", value);
        }
        /// <summary>
        /// Set the CSS width of each element in the set of matched elements.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public CsQuery Height(int value)
        {
            return Height(value.ToString() + "px");
        }
        public CsQuery Height(string value)
        {
            return Css("height", value);
        }

        /// <summary>
        /// Check the current matched set of elements against a selector and return true if at least one of these elements matches the selector.
        /// </summary>
        /// <param name="selector"></param>
        /// <returns></returns>
        public bool Is(string selector)
        {
            return Filter(selector).Length > 0;
            //CsQuerySelectors selectors = new CsQuerySelectors(selector);
            //return !selectors.Select(Dom,Selection).IsNullOrEmpty();
        }
        public bool Is(IEnumerable<IDomObject> elements)
        {
            HashSet<IDomObject> els = new HashSet<IDomObject>(elements);
            els.IntersectWith(Selection);
            return els.Count > 0;
            //CsQuerySelectors selectors = new CsQuerySelectors(elements);
            //return !selectors.Select(Dom, Selection).IsNullOrEmpty();
        }
        public bool Is(IDomObject element)
        {
            return Selection.Contains(element);

            //CsQuerySelectors selectors = new CsQuerySelectors(element);
            //return !selectors.Select(Dom, Selection).IsNullOrEmpty();
        }

      

    }


    
}