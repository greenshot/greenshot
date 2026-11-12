/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System.Linq;
using System.Text.RegularExpressions;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// The window title of Microsoft Edge: "page and 12 more pages - profile - Microsoft\u200B Edge", in the language of Edge.
    /// The texts come from the locale files of Edge 157 (Locales\*.pak, the plural text after "$1 - Microsoft Edge Canary"),
    /// # stands for the number. Edge puts a zero width space between "Microsoft" and " Edge", some languages use – — or : as separator.
    /// </summary>
    internal static class EdgeWindowTitle
    {
        /// <summary>
        /// What Edge adds after the page title when the window has more tabs, # is the number
        /// </summary>
        internal static readonly string[] MorePagesTexts =
        {
            " en nog # bladsy", // af
            " en nog # bladsye", // af
            " እና # ተጨማሪ ገጽ", // am
            " እና # ተጨማሪ ገጾች", // am
            " وصفحة واحدة أخرى", // ar
            " و# من الصفحات الأخرى", // ar
            " আৰু # খন বেছি পৃষ্ঠা", // as
            " və daha # səhifə", // az
            " и още # страница", // bg
            " и още # страници", // bg
            " এবং আরো # টি পৃষ্ঠা", // bn-IN
            " এবং আরও # টি পৃষ্ঠা", // bn-IN
            " i još # stranice", // bs
            " i još # stranica", // bs,hr,sr-Latn-RS
            " i una pàgina més", // ca
            " i # pàgines més", // ca,ca-Es-VALENCIA
            " i # pàgina més", // ca-Es-VALENCIA
            " a # další stránka", // cs
            " a # dalších stránek", // cs
            " ac # dudalen arall", // cy
            " a # o dudalennau eraill", // cy
            " og # side mere", // da
            " og # flere sider", // da
            " und # weitere Seite", // de
            " und # weitere Seiten", // de
            " και # ακόμη σελίδα", // el
            " και # ακόμη σελίδες", // el
            " and # more page", // en-GB,en-US
            " and # more pages", // en-GB,en-US
            " y # página más", // es,es-419
            " y # páginas más", // es,es-419
            " ja veel # leht", // et
            " ja veel # lehte", // et
            " eta beste # orri gehiago", // eu
            " و # صفحه دیگر", // fa
            " ja # muu sivu", // fi
            " ja # muuta sivua", // fi
            " at # pang pahina", // fil
            " et # page supplémentaire", // fr
            " et # page de plus", // fr,fr-CA
            " agus # leathanach eile", // ga
            " agus # duilleag eile", // gd
            " agus # duilleagan eile", // gd
            " e # páxina máis", // gl
            " e # páxinas máis", // gl
            " અને # વધુ પૃષ્ઠ", // gu
            " અને # વધુ પૃષ્ઠો", // gu
            " ועמוד אחד נוסף", // he
            " ו-# דפים נוספים", // he
            " और # अधिक पेज", // hi
            " i još jedna stranica", // hr
            " és # további lap", // hu
            " dan # halaman lainnya", // id
            " og # síða í viðbót", // is
            " og # síður í viðbót", // is
            " e # altra pagina", // it
            " e altre # pagine", // it
            " および他 # ページ", // ja
            " და კიდევ # გვერდი", // ka
            " және тағы # бет", // kk
            " និង # ទំព័របន្ថែម", // km
            " ಮತ್ತು # ಇನ್ನಷ್ಟು ಪುಟ", // kn
            " ಮತ್ತು # ಇನ್ನಷ್ಟು ಪುಟಗಳು", // kn
            " 외 페이지 #개", // ko
            " आनी # आनीक पान", // kok
            " आनी # चड पानां", // kok
            " an nach # Säit", // lb
            " an nach # Säiten", // lb
            " ແລະ # ໜ້າເພີ່ມຕື່ມ", // lo
            " ແລະ # ໜ້າເພີ່ມເຕີມ", // lo
            " ir dar # psl.", // lt
            " un vēl # lapa", // lv
            " un vēl # lapas", // lv
            " me tētahi atu whārangi #", // mi
            " me ētahi atu whārangi #", // mi
            " и уште # страница", // mk
            " и уште # страници", // mk
            " എന്നതിനൊപ്പം # പേജ് കൂടി", // ml
            " ചെയ്ത് # കൂടുതൽ പേജുകൾ", // ml
            " आणि # अधिक पृष्ठ", // mr
            " आणि # अधिक पृष्ठे", // mr
            " dan # halaman lagi", // ms
            " u paġna # aktar", // mt
            " u # paġni aktar", // mt
            " og # side til", // nb,nn
            " og # sider til", // nb,nn
            " र थप # पृष्ठ", // ne
            " र थप # पृष्ठहरू", // ne
            " en nog # andere pagina", // nl
            " en nog # andere pagina's", // nl
            " ଏବଂ # ଅଧିକ ପୃଷ୍ଠା", // or
            " ଏବଂ # ଅଧିକ ପୃଷ୍ଠାଗୁଡିକ", // or
            " ਅਤੇ # ਹੋਰ ਪੰਨਾ", // pa
            " ਅਤੇ # ਹੋਰ ਪੰਨੇ", // pa
            " i jeszcze # strona", // pl
            " i dodatkowe strony: #", // pl
            " e mais # página", // pt-BR,pt-PT
            " e mais # páginas", // pt-BR,pt-PT
            " chaymanta # aswan rapi", // qu
            " chaymanta # aswan rapikuna", // qu
            " și încă # pagină", // ro
            " și încă # pagini", // ro
            " и еще # страница", // ru
            " и еще # страницы", // ru
            " и еще # страниц", // ru
            " a # ďalšia stránka", // sk
            " a # ďalšie stránky", // sk
            " in še # stran", // sl
            " dhe # faqe tjetër", // sq
            " dhe # faqe të tjera", // sq
            " и још # страница", // sr,sr-Cyrl-BA
            " och # sida till", // sv
            " och # sidor till", // sv
            " மற்றும் # கூடுதல் பக்கம்", // ta
            " மற்றும் # கூடுதல் பக்கங்கள்", // ta
            " మరియు # మరొక పేజీ", // te
            " మరియు # మరిన్ని పేజీలు", // te
            " และอีก # เพจ", // th
            " ve diğer # sayfa", // tr
            " һәм тагын # бит", // tt
            " ۋە يەنە # بەت", // ug
            " і ще # сторінка", // uk
            " і ще # сторінок", // uk
            " اور # مزید صفحہ", // ur
            " اور # مزید صفحات", // ur
            " và # trang nữa", // vi
            " và # trang khác", // vi
            " 和另外 # 个页面", // zh-CN
            " 和其他 # 個頁面", // zh-TW
        };

        /// <summary>
        /// The regular expression of the title fix: the text about the other pages, the profile name and the browser name
        /// </summary>
        internal static string Matcher { get; } = BuildMatcher();

        private static string BuildMatcher()
        {
            var morePages = MorePagesTexts
                .Distinct()
                .OrderByDescending(text => text.Length)
                .Select(text => Regex.Escape(text).Replace(@"\#", @"\d+").Replace(@"\ ", @"\s"));
            const string separator = @"\s?[-\u2013\u2014:]\s?";
            return $@"(?:{string.Join("|", morePages)})?(?:{separator}[^-\u2013\u2014:]+?)?{separator}\u201E?Microsoft\u200B?\sEdge.*";
        }
    }
}
