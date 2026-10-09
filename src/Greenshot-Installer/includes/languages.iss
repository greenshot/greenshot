[Languages]
Name: en; MessagesFile: compiler:Default.isl
Name: cn; MessagesFile: Languages\ChineseSimplified.isl
Name: de; MessagesFile: compiler:Languages\German.isl
Name: es; MessagesFile: compiler:Languages\Spanish.isl
Name: fi; MessagesFile: compiler:Languages\Finnish.isl
Name: fr; MessagesFile: compiler:Languages\French.isl
Name: it; MessagesFile: compiler:Languages\Italian.isl
Name: nl; MessagesFile: compiler:Languages\Dutch.isl
Name: lv; MessagesFile: Languages\Latvian.isl
Name: nn; MessagesFile: Languages\NorwegianNynorsk.isl
Name: ptBR; MessagesFile: compiler:Languages\BrazilianPortuguese.isl
Name: ru; MessagesFile: compiler:Languages\Russian.isl
Name: sr; MessagesFile: Languages\SerbianCyrillic.isl
Name: sv; MessagesFile: compiler:Languages\Swedish.isl
Name: tr; MessagesFile: compiler:Languages\Turkish.isl
Name: uk; MessagesFile: compiler:Languages\Ukrainian.isl

#include "..\scripts\lcid.iss"

[Files]
; Additional language files
Source: {#LanguagesDir}\*ar-SY*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\arSY; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*ca-CA*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\caCA; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*cs-CZ*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\csCZ; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*da-DK*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\daDK; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*de-x-franconia*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\dexfranconia; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*el-GR*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\elGR; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*es-ES*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\esES; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*et-EE*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\etEE; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*fa-IR*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\faIR; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*fi-FI*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\fiFI; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*fr-FR*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\frFR; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*fr-QC*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\frQC; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*he-IL*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\heIL; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*hu-HU*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\huHU; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*id-ID*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\idID; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*it-IT*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\itIT; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*ja-JP*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\jaJP; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*ko-KR*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\koKR; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*kab-DZ*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\kabDZ; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*lt-LT*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\ltLT; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*lv-LV*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\lvLV; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*nn-NO*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\nnNO; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*pl-PL*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\plPL; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*pt-BR*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\ptBR; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*pt-PT*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\ptPT; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*ro-RO*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\roRO; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*ru-RU*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\ruRU; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*sk-SK*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\skSK; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*sl-SI*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\slSI; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*sr-RS*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\srRS; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*sv-SE*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\svSE; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*tr-TR*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\trTR; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*uk-UA*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\ukUA; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*va-VA*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\vaVA; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*vi-VN*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\viVN; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*zh-CN*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\zhCN; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*zh-TW*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\zhTW; Flags: {#DefaultInstallFlags};

[Components]
Name: "languages"; Description: {cm:language}; Types: full custom; Flags: disablenouninstallwarning
Name: "languages\arSY"; Description: {cm:arSY}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('d')
Name: "languages\caCA"; Description: {cm:caCA}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\csCZ"; Description: {cm:csCZ}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\daDK"; Description: {cm:daDK}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\dexfranconia"; Description: {cm:dexfranconia}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\elGR"; Description: {cm:elGR}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('4')
Name: "languages\esES"; Description: {cm:esES}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\etEE"; Description: {cm:etEE}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('2')
Name: "languages\faIR"; Description: {cm:faIR}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('d')
Name: "languages\fiFI"; Description: {cm:fiFI}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\frFR"; Description: {cm:frFR}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\frQC"; Description: {cm:frQC}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\heIL"; Description: {cm:heIL}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('c')
Name: "languages\huHU"; Description: {cm:huHU}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('2')
Name: "languages\idID"; Description: {cm:idID}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\itIT"; Description: {cm:itIT}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\jaJP"; Description: {cm:jaJP}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('7')
Name: "languages\kabDZ"; Description: {cm:kabDZ}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('8')
Name: "languages\koKR"; Description: {cm:koKR}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('8')
Name: "languages\ltLT"; Description: {cm:ltLT}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('3')
Name: "languages\lvLV"; Description: {cm:lvLV}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('3')
Name: "languages\nnNO"; Description: {cm:nnNO}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\plPL"; Description: {cm:plPL}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('2')
Name: "languages\ptBR"; Description: {cm:ptBR}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\ptPT"; Description: {cm:ptPT}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\roRO"; Description: {cm:roRO}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('2')
Name: "languages\ruRU"; Description: {cm:ruRU}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('5')
Name: "languages\skSK"; Description: {cm:skSK}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('2')
Name: "languages\slSI"; Description: {cm:slSI}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('2')
Name: "languages\srRS"; Description: {cm:srRS}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('5')
Name: "languages\svSE"; Description: {cm:svSE}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\trTR"; Description: {cm:trTR}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('6')
Name: "languages\ukUA"; Description: {cm:ukUA}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('5')
Name: "languages\vaVA"; Description: {cm:vaVA}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\viVN"; Description: {cm:viVN}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('e')
Name: "languages\zhCN"; Description: {cm:zhCN}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('a')
Name: "languages\zhTW"; Description: {cm:zhTW}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('9')

[CustomMessages]
language=Additional languages
en.language=Additional languages
de.language=Zusätzliche Sprachen
es.language=Idiomas adicionales
fi.language=Lisäkielet
fr.language=Langues additionnelles
it.language=Lingue aggiuntive
lv.language=Papildus valodas
nl.language=Extra talen
nn.language=Andre språk
ptBR.language=Idiomas adicionais
ru.language=Дополнительные языки
sr.language=Додатни језици
sv.language=Ytterligare språk
tr.language=Ek diller
uk.language=Додаткові мови
cn.language=其它语言

;Language names in the original language
dexfranconia=Frängisch (Deutsch)
arSY=العربية
caCA=Català
csCZ=Čeština
daDK=Dansk
elGR=ελληνικά
esES=Español
etEE=Eesti
faIR=پارسی
fiFI=Suomi
frFR=Français
frQC=Français - Québec
heIL=עִבְרִית
huHU=Magyar
idID=Bahasa Indonesia
itIT=Italiano
jaJP=日本語
kabDZ=Taqbaylit
koKR=한국어
ltLT=Lietuvių
lvLV=Latviski
nnNO=Nynorsk
plPL=Polski
ptBR=Português do Brasil
ptPT=Português de Portugal
roRO=Română
ruRU=Pусский
skSK=Slovenčina
slSI=Slovenščina
srRS=Српски
svSE=Svenska
trTR=Türkçe
ukUA=Українська
vaVA=Valencià
viVN=Việt
zhCN=简体中文
zhTW=繁體中文

it.dexfranconia=Fräncofono (Tedesco)
it.arSY=Arabo (Siria)
it.caCA=Catalano
it.csCZ=Ceco
it.daDK=Danese
it.elGR=Greco
it.esES=Spagnolo
it.etEE=Eesti
it.faIR=Farsi (Iran)
it.fiFI=Suomi
it.frFR=Francese
it.frQC=Francese (Québec)
it.heIL=Ebraico (Israele)
it.huHU=Ungherese
it.idID=Bahasa Indonesia
it.itIT=Italiano
it.jaJP=Giapponese
it.kabDZ=Taqbaylit
it.koKR=Coreano
it.ltLT=Lituano
it.lvLV=Latviano
it.nnNO=Norvegese
it.plPL=Polacco
it.ptBR=Portoghese (Brasile)
it.ptPT=Portoghese (Portogallo)
it.roRO=Rumeno
it.ruRU=Russo
it.skSK=Slovacco
it.slSI=Sloveno
it.srRS=Serbo (Russia)
it.svSE=Svedese
it.trTR=Türco
it.ukUA=Ucraino
it.viVN=Vietnamita
it.zhCN=Cinese (Semplificato)
it.zhTW=Cinese (Taiwan)
