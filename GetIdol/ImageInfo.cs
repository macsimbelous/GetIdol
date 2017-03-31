/* Copyright © Macsim Belous 2013 */
/* This file is part of ErzaLib.

    Foobar is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Foobar is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Foobar.  If not, see <http://www.gnu.org/licenses/>.*/
using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace ErzaLib
{
    public class ImageInfo
    {
        public bool IsDeleted = false;
        public long ImageID = -1;
        public string Hash = null;
        public string FilePath = null;
        public int Width = 0;
        public int Height = 0;
        public List<string> Tags = new List<string>();
        public string GetStringOfTags()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < this.Tags.Count; i++)
            {
                if (i == 0)
                {
                    sb.Append(this.Tags[i]);
                }
                else
                {
                    sb.Append(' ');
                    sb.Append(this.Tags[i]);
                }
            }
            return sb.ToString();
        }
        public void AddTag(string Tag)
        {
            if ((Tag != null) && (Tag != String.Empty))
            {
                if (this.Tags.LastIndexOf(Tag) < 0)
                {
                    this.Tags.Add(Tag);
                }
            }
        }
        public void AddTags(string[] Tags)
        {
            foreach (string tag in Tags)
            {
                if ((tag != null) && (tag != String.Empty))
                {
                    if (this.Tags.LastIndexOf(tag) < 0)
                    {
                        this.Tags.Add(tag);
                    }
                }
            }
        }
        public void AddTags(List<string> Tags)
        {
            foreach (string tag in Tags)
            {
                if ((tag != null) && (tag != String.Empty))
                {
                    if (this.Tags.LastIndexOf(tag) < 0)
                    {
                        this.Tags.Add(tag);
                    }
                }
            }
        }
        public void AddStringOfTags(string TagsString)
        {
            string[] tags_array = TagsString.Split(' ');
            foreach (string tag in tags_array)
            {
                if ((tag != null) && (tag != String.Empty))
                {
                    if (this.Tags.LastIndexOf(tag) < 0)
                    {
                        this.Tags.Add(tag);
                    }
                }
            }
        }
        public override string ToString()
        {
            if (this.FilePath != String.Empty)
            {
                return FilePath.Substring(FilePath.LastIndexOf('\\') + 1);
            }
            else
            {
                return "No File!";
            }
        }
    }
}
