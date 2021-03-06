﻿using System.Collections.Generic;
using System.ComponentModel;
using UploadersLib;

namespace ZScreenLib
{
    public class FtpAccountListTypeConverter : TypeConverter
    {
        private static List<FTPAccount> mList = Engine.ConfigUploaders.FTPAccountList2;

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }

        private StandardValuesCollection GetValues()
        {
            return new StandardValuesCollection(mList);
        }

        public static void SetList(List<FTPAccount> list)
        {
            mList = list;
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return GetValues();
        }
    }
}