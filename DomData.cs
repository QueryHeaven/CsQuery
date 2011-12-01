﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Jtc.CsQuery
{
    public static class DomData
    {
        // when false, will use binary data for the character set

#if DEBUG_PATH
        public static bool Debug = true;
        public const int pathIdLength = 3;
        public const char indexSeparator = '>';
#else
        public static bool Debug = false;
        public const int pathIdLength = 1;
        public const char indexSeparator = (char)1;
#endif
        /// <summary>
        /// Length of each node's path ID, sets a limit on the number of child nodes before a reindex
        /// is required. For most cases, a small number will yield better performance.
        /// </summary>

        /// <summary>
        /// Hardcode some token IDs to improve performance for frequent lookups
        /// </summary>
        public const ushort StyleAttrId = 2;
        public const ushort ClassAttrId = 3;
        public const ushort ValueAttrId=4;
        public const ushort IDAttrId=5;
        public const ushort ScriptNodeId = 6;
        public const ushort TextareaNodeId = 7;
        public const ushort InputNodeId = 8;

        public static ushort SelectedAttrId;
        public static ushort ReadonlyAttrId;
        public static ushort CheckedAttrId;
        
        // HTML spec for whitespace
        // U+0020 SPACE, U+0009 CHARACTER TABULATION (tab), U+000A LINE FEED (LF), U+000C FORM FEED (FF), and U+000D CARRIAGE RETURN (CR).
        public static char[] Whitespace = new char[] { '\x0020', '\x0009', '\x000A', '\x000C', '\x000D' };
        // U+0022 QUOTATION MARK characters ("), U+0027 APOSTROPHE characters ('), U+003D EQUALS SIGN characters (=), 
        // U+003C LESS-THAN SIGN characters (<), U+003E GREATER-THAN SIGN characters (>), or U+0060 GRAVE ACCENT characters (`),
        // and must not be the empty string.}
        public static char[] MustBeQuoted = new char[] { '\x0022', '\x0027', '\x003D', '\x003C', '\x003E', '\x0060' };
        public static char[] MustBeQuotedAll;

        // things that can be in a css number
        public static HashSet<char> NumberChars = new HashSet<char>("-+0123456789.,");
        public static HashSet<string> Units = new HashSet<string>(new string[] { "%", "in", "cm", "mm", "em", "ex", "pt", "pc", "px" });

        private static ushort noInnerHtmlIDFirst;
        private static ushort noInnerHtmlIDLast;
        private static ushort booleanFirst;
        private static ushort booleanLast;
        private static ushort blockFirst;
        private static ushort blockLast;

        static DomData()
        {
            // For path encoding
            if (!Debug)
            {
                baseXXchars = new char[65534];
                for (ushort i = 0; i < 65534; i++)
                {
                    baseXXchars[i] = (char)(i+1);
                }
            }
            encodingLength = baseXXchars.Length;
            defaultPadding="";
            for (int i = 1; i < pathIdLength; i++)
            {
                defaultPadding=defaultPadding+"0";
            }
            maxPathIndex = (int)Math.Pow(encodingLength,pathIdLength) -1;

            MustBeQuotedAll = new char[Whitespace.Length + MustBeQuoted.Length];
            MustBeQuoted.CopyTo(MustBeQuotedAll, 0);
            Whitespace.CopyTo(MustBeQuotedAll, MustBeQuoted.Length);

            HashSet<string> noInnerHtmlAllowed = new HashSet<string>(new string[]{
            "base","basefont","frame","link","meta","area","col","hr","param","script","textarea",
                "img","input","br", "!doctype","!--"
            });
    
            HashSet<string> blockElements = new HashSet<string>(new string[]{"body","br","address","blockquote","center","div","dir","form","frameset","h1","h2","h3","h4","h5","h6","hr",
                "isindex","li","noframes","noscript","object","ol","p","pre","table","tr","textarea","ul",
                // html5 additions
                "article","aside","button","canvas","caption","col","colgroup","dd","dl","dt","embed","fieldset","figcaption",
                "figure","footer","header","hgroup","object","progress","section","tbody","thead","tfoot","video"
            });

            HashSet<string> booleanAttributes = new HashSet<string>(new string[] {
            "autobuffer", "autofocus", "autoplay", "async", "checked", "compact", "controls", "declare", "defaultmuted", "defaultselected",
            "defer", "disabled", "draggable", "formNoValidate", "hidden", "indeterminate", "ismap", "itemscope","loop", "multiple",
            "muted", "nohref", "noresize", "noshade", "nowrap", "novalidate", "open", "pubdate", "readonly", "required", "reversed",
            "scoped", "seamless", "selected", "spellcheck", "truespeed"," visible"
            });

            TokenIDs = new Dictionary<string, ushort>();
            TokenID("style"); //2
            TokenID("class"); //3
            // inner text allowed
            TokenID("value"); //4
            TokenID("id"); //5

            noInnerHtmlIDFirst = nextID;
            TokenID("script"); //6
            TokenID("textarea"); //7
            TokenID("input"); //8
            
            // no inner html allowed
            
            foreach (string tag in noInnerHtmlAllowed)
            {
                TokenID(tag);
            }
            noInnerHtmlIDLast = (ushort)(nextID - 1);
            booleanFirst = (ushort)nextID;
            foreach (string tag in booleanAttributes)
            {
                TokenID(tag);
            }
            SelectedAttrId= TokenID("selected"); 
            ReadonlyAttrId = TokenID("readonly"); 
            CheckedAttrId = TokenID("checked"); 
            booleanLast = (ushort)(nextID - 1);
            blockFirst = (ushort)nextID;
            foreach (string tag in blockElements)
            {
                TokenID(tag);
            }
            blockLast = (ushort)(nextID - 1);
        }


        private static ushort nextID=2;

        private static List<string> Tokens = new List<string>();
        
        private static Dictionary<string, ushort> TokenIDs;
        private static object locker=new Object();
        public static IEnumerable<string> Keys
        {
            get
            {
                return Tokens;
            }
        }
        /// <summary>
        /// This type does not allow HTML children. Some of these types may allow text but not HTML.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public static bool NoInnerHtmlAllowed(ushort nodeId)
        {
            return nodeId >= noInnerHtmlIDFirst &&
                nodeId <= noInnerHtmlIDLast;
        }
        public static bool NoInnerHtmlAllowed(string nodeName)
        {
            return NoInnerHtmlAllowed(TokenID(nodeName,true));
        }
        /// <summary>
        /// Text is allowed within this node type. Is includes all types that also permit HTML.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public static bool InnerTextAllowed(ushort nodeId)
        {
            return nodeId == ScriptNodeId || nodeId == TextareaNodeId || !NoInnerHtmlAllowed(nodeId);
        }
        public static bool InnerTextAllowed(string nodeName)
        {
            return InnerTextAllowed(TokenID(nodeName,true));
        }
        public static bool IsBlock(ushort nodeId)
        {
            return nodeId >= blockFirst
                && nodeId <= blockLast;
        }
        public static bool IsBlock(string nodeName)
        {
            return IsBlock(TokenID(nodeName,true));
        }
        /// <summary>
        /// The attribute is a boolean type
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public static bool IsBoolean(ushort nodeId)
        {
            return nodeId >= booleanFirst && nodeId <= booleanLast;
        }
        /// <summary>
        /// The attribute is a boolean type
        /// </summary>
        /// <param name="nodeName"></param>
        /// <returns></returns>
        public static bool IsBoolean(string nodeName)
        {
            return IsBoolean(TokenID(nodeName,true));
        }
        /// <summary>
        /// Return a token ID for a name, adding to the index if it doesn't exist.
        /// </summary>
        /// <param name="tokenName"></param>
        /// <param name="toLower"></param>
        /// <returns></returns>
        public static ushort TokenID(string tokenName, bool toLower = false)
        {
            ushort id;
            if (toLower) {
                tokenName = tokenName.ToLower();
            }

            if (!TokenIDs.TryGetValue(tokenName, out id))
            {
                
                lock(locker) {

                    Tokens.Add(tokenName);
                    TokenIDs.Add(tokenName, nextID);
                    // if for some reason we go over 65,535, will overflow and crash. no need 
                    // to check
                    id = nextID++;
                   
                }
            }
            return id;
        }
        /// <summary>
        /// Return a token name for an ID.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static string TokenName(ushort id)
        {
            return id <= 0 ? "" : Tokens[id-2];
        }

        #region Path Encoding

        private static string defaultPadding;
        private static char[] baseXXchars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToArray();
        private static int encodingLength; // set in constructor
        private static int maxPathIndex;
        // The length of each path key - this sets an upper limit on the number of subelements
        // that can be added without reindexing the node

        public static string BaseXXEncode(int number)
        {

            // optimize for small numbers - this should mostly eliminate the ineffieciency of base62
            // encoding while giving benefits for storage space
            if (number < encodingLength)
            {
                return defaultPadding +baseXXchars[number];
            }
            if (number > maxPathIndex)
            {
                throw new Exception("Maximum number of child nodes (" + maxPathIndex + ") exceeded."); 
            }
            string sc_result = "";
            int num_to_encode = number;
            int i = 0;
            do
            {
                i++;
                sc_result = baseXXchars[(num_to_encode % encodingLength)] + sc_result;
                num_to_encode = ((num_to_encode - (num_to_encode % encodingLength)) / encodingLength);
                
            }
            while (num_to_encode != 0);
            
            return sc_result.PadLeft(pathIdLength, '0');
        }
        #endregion

    }
}