﻿#region License Information (GPL v2)

/*
    ZScreen - A program that allows you to upload screenshots in one keystroke.
    Copyright (C) 2008-2011 ZScreen Developers

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v2)

using System.Collections.Generic;

namespace UploadersLib.URLShorteners
{
    public sealed class ThreelyURLShortener : URLShortener
    {
        private const string APIURL = "http://3.ly";

        private string APIKey;

        public override string Host
        {
            get
            {
                return "3.ly";
            }
        }

        public ThreelyURLShortener(string key)
        {
            APIKey = key;
        }

        public override string ShortenURL(string url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                Dictionary<string, string> arguments = new Dictionary<string, string>();
                arguments.Add("api", APIKey);
                arguments.Add("u", url);

                return SendGetRequest(APIURL, arguments);
            }

            return null;
        }
    }
}