using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ManualPatcher
{
    class LocaleEurope : BaseLocale
    {
        public LocaleEurope()
            //: base("Europe", "http://patch.nexoneu.com/maple/patch/patchdir/", 9, 69)
            : base("Europe", "http://nxeu-proxy.craftnet.nl/maple/patch/patchdir/", 9, 69)
        {

        }
    }

    class LocaleGlobal : BaseLocale
    {
        public LocaleGlobal()
            //: base("Global", "http://download2.nexon.net/Game/MapleStory/patch/patchdir/", 8, 71)
            : base("Global", "http://nx-proxy.craftnet.nl/Game/MapleStory/patch/patchdir/", 8, 71)
        {
        }
    }


    class LocaleSEA : BaseLocale
    {
        public LocaleSEA()
            : base("Global", "ftp://update.maplesea.com/sea/patch/patchdir/", 7, 139)
        {
        }
    }

    //
}
