using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MS_AutoPatcher
{
    class LocaleEurope : BaseLocale
    {
        public LocaleEurope()
            : base("Europe", "http://patch.nexoneu.com/maple/patch/", 9, 69)
        {
            AddProxy("CraftNet NL Proxy", "http://nxeu-proxy.craftnet.nl/maple/patch/");
            AddProxy("CraftNet NL Mirror", "http://nx-mirror.craftnet.nl/ems/maple/patch/");
        }
    }

    class LocaleGlobal : BaseLocale
    {
        public LocaleGlobal()
            : base("Global", "http://download2.nexon.net/Game/MapleStory/patch/", 8, 71)
        {
            AddProxy("CraftNet USA Proxy", "http://nx-proxy.craftnet.nl/Game/MapleStory/patch/");
            AddProxy("CraftNet NL Mirror", "http://nx-mirror.craftnet.nl/gms/Game/MapleStory/patch/");
        }
    }


    class LocaleSEA : BaseLocale
    {
        public LocaleSEA()
            : base("SEA", "http://update.maplesea.com/sea/patch/", 7, 141, 3)
        {
            AddProxy("Official (FTP)", "ftp://update.maplesea.com/sea/patch/");
            AddProxy("CraftNet NL Mirror", "http://nx-mirror.craftnet.nl/sea/sea/patch/");
        }
    }

    class LocaleJapan : BaseLocale
    {
        public LocaleJapan()
            : base("Japan", "http://webdown2.nexon.co.jp/maple/patch/", 3, 318)
        {
            AddProxy("Official (FTP)", "ftp://download2.nexon.co.jp/maple/patch/");
            AddProxy("CraftNet NL Mirror", "http://nx-mirror.craftnet.nl/jms/");
        }
    }
    
    class LocaleTaiwan : BaseLocale
    {
        public LocaleTaiwan()
            : base("Taiwan", "ftp://tw.patch.maplestory.gamania.com/maplestory/patch/", 6, 177)
        {
            AddProxy("CraftNet NL Mirror", "http://nx-mirror.craftnet.nl/twms/");
        }
    }

    class LocaleIndonesia : BaseLocale
    {
        public LocaleIndonesia()
            : base("Indonesia", "http://202.93.17.225/", 100, 1)
        {
            AddProxy("CraftNet NL Mirror", "http://nx-mirror.craftnet.nl/ims/");
            NewFormat = true;
        }
    }

    class LocaleKorea : BaseLocale
    {
        public LocaleKorea()
            : base("Korea", "http://maplestory.dn.nexoncdn.co.kr/Patch/", 1, 230)
        {
            AddProxy("CraftNet NL Mirror", "http://nx-mirror.craftnet.nl/kms/Patch/");
            NewFormat = true;
        }
    }

    class LocaleChina : BaseLocale
    {
        public LocaleChina()
            : base("China", "http://mxd.clientdown.sdo.com/mxd/Patch/", 4, 123)
        {
            AddProxy("CraftNet NL Mirror", "http://nx-mirror.craftnet.nl/cms/mxd/Patch/");
        }
    }
}
