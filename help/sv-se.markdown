---
layout: page
status: publish
published: true
title: Greenshot Hjälp
permalink: /help/sv-se/
categories: []
tags: []
comments: []
---
<div class="pull-right">{% include help-nav.html %}</div>

<p><small>Version 1.2.10 - Svensk översättning av hjälpinnehållet av Torsten Augustsson</small> - Redigera denna sida under <a href="https://github.com/greenshot/greenshot/blob/gh-pages/help/sv-se.markdown">Github</a></p>

<h2>Innehåll</h2>
<ol>
<li><a href="#screenshot">Skapa en skärmdump</a></li>
<ol>
<li><a href="#capture-region">Skärmdump av yta</a></li>
<li><a href="#capture-last-region">Skärmdump av senaste ytan</a></li>
<li><a href="#capture-window">Skärmdump av fönster</a></li>
<li><a href="#capture-fullscreen">Skärmdump av hela skärmen</a></li>
<li><a href="#capture-ie">Skärmdump av Internet Explorer</a></li>
</ol>

<li><a href="#editor">Använda bildredigeraren</a></li>
<ol>
<li><a href="#editor-shapes">Rita former</a></li>
<li><a href="#editor-text">Lägga till text</a></li>
<li><a href="#editor-highlight">Framhäva saker</a></li>
<li><a href="#editor-obfuscate">Censurera saker</a></li>
<li><a href="#editor-crop">Beskära skärmdumpen</a></li>
<li><a href="#editor-enlarge">Förstora skärmdumpen</a></li>
<li><a href="#editor-adding-graphics">Lägga till grafik till en skärmdump</a></li>
<li><a href="#editor-reuse-elements">Återanvända ritade element</a></li>
<li><a href="#editor-export">Exportera skärmdumpen</a></li>
</ol>
<li><a href="#settings">Inställningarna</a></li>
<ol>
<li><a href="#settings-general">Allmänt</a></li>
<li><a href="#settings-capture">Skärmdump</a></li>
<li><a href="#settings-output">Utdata</a></li>
<li><a href="#settings-printer">Utskrift</a></li>
</ol>
<li><a href="#help">Vill du hjälpa till?</a></li>
<ol>
<li><a href="#help-donate">Överväg en donation</a></li>
<li><a href="#help-spread">Sprid ordet</a></li>
<li><a href="#help-translate">Lägg till en översättning</a></li>	
</ol>
</ol>

<p><a name="preface"></a></p>
<h2>Preface</h2>
<p>
Greenshot körs som en egen process i bakgrunden. För att nå applikationen kan du högerklicka på Greenshot-ikonen i meddelandefältet eller direkt ta en <a href="#screenshot">skärmdump</a> genom att trycka på en av de definierade tangenterna. Flera redigeringsfönster kan öppnas parallellt.
</p>
	
<p><a name="screenshot"></a></p>
<h2>Skapa en skärmdump</h2>
<p>
Du kan skapa en skärmdump antingen genom att använda <kbd>Print</kbd>-tangenten på ditt tangentbord, eller genom att högerklicka på Greenshot-ikonen i aktivitetsfältet.<br>
Du kan skapa en skärmdump på flera olika sätt:
</p>

<p><a name="capture-region"></a></p>
<h3>Skärmdump av yta <kbd>Print</kbd></h3>
<p>
Det här alternativet låter dig ta en skärmdump av en del av din skärm.<br>
Efter du startat detta läge kommer du se ett sikte istället för din muspekare. Klicka och håll in musknappen där du vill att hörnet av skärmdumpen ska vara. Genom att dra musen, när du fortfarande håller ner musknappen, ritar du ut en rektangel. Släpp musknappen när den gröna rektangeln täcker den yta du vill ha med i skärmdumpen.
</p>
<p class="hint">
Du kan använda tangenten <kbd>Mellanslag</kbd> för att byta läge mellan yta och <a href="#capture-window">fönster</a>.<br />
Om du håller ned tangenten <kbd>Skift</kbd> medan du ritar ut ytan fixeras en dimension av markeringsrektangeln.
</p>
<p class="hint">
Om du vill att skärmdumpen ska innehålla en exakt del av skärmen, kan du använda <kbd>Pil</kbd>-tangenterna för att justera muspekarens position per pixel, eller per 10 pixlar om du håller ner tangenten <kbd>Ctrl</kbd>. Tryck på <kbd>Enter</kbd> för att tillämpa start-/slutpositionen för den markerade ytan. Du kan växla mellan att visa förstoringsglaset genom att trycka ner <kbd>Z</kbd>.
</p>

<p><a name="capture-last-region"></a></p>
<h3>Skärmdump av senaste ytan <kbd>Shift</kbd> + <kbd>Print</kbd></h3>
<p>
Om du tidigare tagit en skärmdump av en <a href="#capture-region">yta</a> eller ett <a href="#capture-window">fönster</a>, kan du ta en skärmdump av samma del av skärmen med detta alternativ.
</p>

<p><a name="capture-window"></a></p>
<h3>Skärmdump av fönster <kbd>Alt</kbd> + <kbd>Print</kbd></h3>
<p>
Skapar en skärmdump av det fönster som är aktivt.
</p>
<p class="hint">
I <a href="#settings">inställningarna</a> kan du ställa in så att skärmdumpen inte tas av det aktiva fönstret med en gång, utan låter dig välja fönster istället.
Om detta alternativ är valt kan du välja ett fönster genom att klicka på det (likadant som vid en <a href="#capture-region">skärmdump av yta</a> kommer Greenshot framhäva området).<br>
Om du vill ta en skärmdump av ett underfönster (t.ex. en webbläsares visningsområde, utan verktygsfält o.s.v., eller en ensam ram i en webbsida som använder ramar), placera muspekaren över fönstret och tryck på tangenten <kbd>PgDown</kbd>. Efter det kan du välja delar av fönstret till din skärmdump.
</p>
<p class="hint">
Att ta en skärmdump av enbart snabbmenyer är annorlunda. Om använder kortkommandot för att ta en skärmdump av ett fönster försvinner snabbmenyn, och detsamma skulle hända om du använde Greenshots snabbmeny för att ta skärmdumpen. Om du vill ta en skärmdump av en snabbmeny du fått fram genom att högerklicka på någonting, klicka på <kbd>Print</kbd> och sedan på <kbd>Mellanslag</kbd>.
</p>

<p><a name="capture-fullscreen"></a></p>
<h3>Skärmdump av hela skärmen <kbd>Ctrl</kbd> + <kbd>Print</kbd></h3>
<p>
Skapar en skärmdump av hela skärmen.
</p>

<p><a name="capture-ie"></a></p>
<h3>Skärmdump av Internet Explorer <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>Print</kbd></h3>
<p>
Skapa enkelt en skärmdump av en öppen webbsida i Internet Explorer.
Använd Greenshots snabbmeny för att välja flik i Internet Explorer, eller klicka på <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>Print</kbd> för att ta en skärmdump av den aktiva fliken.
</p>

<p><a name="editor"></a></p>
<h2>Använda bildredigeraren</h2>
<p>
Greenshot inkluderar en lättanvänd bildredigerare som ger dig praktiska verktyg för att lägga till kommentarer eller former på en skärmdump. Den låter dig till och med framhäva eller censurera delar av din skärmdump.
</p>
<p class="hint">
Greenshots bildredigerare kan användas till fler saker än skärmdumpar. Du kan också öppna bilder att redigera från filer eller urklippet. Högerklicka på Greenshot-ikonen i aktivitetsfältet och välj <em>Öppna bild från fil</em> eller <em>Öppna bild från urklipp</em>.
</p>
<p class="hint">
Som standard kommer bildredigeraren öppnas när du tar en skärmdump. Om du inte vill använda bildredigeraren kan du stänga av denna funktion i <a href="#settings">inställningarna</a>.
</p>
<p class="hint">
Om ett eller flera redigeringsfönster redan är öppna och Greenshot är konfigurerat att öppna
destinationsväljaren för nya skärmdumpar, kan du hålla muspekaren
över alternativet <em>Öppna i bildredigerare</em> för att visa en lista över alla öppna redigeringsfönster
att välja ifrån. Den nya skärmdumpen kommer att infogas som ett separat objekt i den valda redigeraren.
</p>

<a name="editor-shapes"></a>
<h3>Rita former</h3>
<p>
Välj ett av verktygen för att rita former från verktygsraden i vänstra delen av bildredigeraren, eller från menyn <em>Objekt</em>. Det finns också en snabbknapp du kan använda för varje verktyg.<br />
De tillgängliga formerna är: rektangel <kbd>R</kbd>, cirkel <kbd>E</kbd>, linje<kbd>L</kbd>,
pil <kbd>A</kbd> och rita på frihand <kbd>F</kbd>.<br />
Klicka, håll nere musknappen och dra för att bestämma position och storlek av formen. Släpp musknappen när du är klar.
</p>
<p>
Du kan flytta eller ändra storlek på existerande former om du markerar dem med markeringsverktyget <kbd>Esc</kbd> från verktygsraden.<br>
För varje elementtyp finns ett antal alternativ tillgängliga för att ändra utseendet på elementet (t.ex. linjetjocklek, linjefärg och fyllnadsfärg). Du kan ändra alternativen för ett specifikt element genom att markera det, men också för nästa element som ritas efter att ha valt ett ritverktyg.
</p>
<p class="hint">
För att välja en färg vid färgväljaren med pipetten, tryck på pipetten och flytta runt musen medan du fortfarande håller ned vänster musknapp. På så sätt kan du välja en färg någonstans från hela skärmen, inte bara Greenshot.
</p>
<p class="hint">
Du kan välja flera element att redigera eller flytta åt gången. För att markera flera element, håll ner tangenten <kbd>Shift</kbd> när du klickar på elementen.
</p>
<p class="hint">
Om du vill rita liksidiga former (t.ex. tvinga en rektangel att vara en kvadrat), håll ner <kbd>Shift</kbd> när du ritar. När du ritar linjer eller pilar kommer linjens vinkel stegas i grader om 15° när du håller ner <kbd>Shift</kbd>.<br>
Du kan också använda <kbd>Shift</kbd> om du vill ändra storlek på ett existerande objekt och behålla dess proportioner.
</p>
<p class="hint">
När du ritar eller skalar objekt kan du hålla nere <kbd>Ctrl</kbd> för att objektet ska ankras i dess geometriska mitt, d.v.s. att objektet också ändrar storlek i motsatt riktning. (Detta är väldigt praktiskt om du vill rita en cirkel runt någonting på din skärmdump.)
</p>

<p><a name="editor-text"></a></p>
<h3>Lägga till text</h3>
<p>
Att använda textverktyget <kbd>T</kbd> är likt användandet av verktygen för att <a href="#editor-shapes">rita former</a>.
Rita helt enkelt ut textelementet till önskad storlek, och skriv sedan in din text.<br />
Dubbelklicka på ett existerande textelement för att redigera text.<br />
Tryck på <kbd>Return</kbd> eller <kbd>Enter</kbd> när du är klar med redigeringen.
</p>
<p class="hint">
Om du behöver lägga till radbrytningar i en textruta, tryck på <kbd>Shift</kbd> + <kbd>Return</kbd> eller <kbd>Shift</kbd> + <kbd>Enter</kbd>. <kbd>Ctrl</kbd> + <kbd>Backsteg</kbd> raderar föregående ord, <kbd>Ctrl</kbd> + <kbd>A</kbd> markerar all text.
</p>

<p><a name="editor-highlight"></a></p>
<h3>Framhäva saker</h3>
<p>
Efter att du valt framhävningsverktyget <kbd>H</kbd> kan du definiera ytan du vill ska framhävas exakt på samma sätt du ritar en <a href="#editor-shapes">form</a>.<br />
Det finns flera olika alternativ för framhävning, som du kan välja mellan genom att klicka på knappen längst till vänster i verktygsfältet topp:
</p>
<ul>
<li><em>Framhäv text</em>: framhäver ett område genom att lägga till en stark färg, som en överstrykningspenna</li>
<li><em>Framhäv område</em>: lägger till oskärpa<a href="#hint-blur">*</a> och mörklägger allting utanför det markerade området</li>
<li><em>Gråskala</em>: allting utanför det markerade området visas i gråskala</li>
<li><em>Förstora</em>: det valda området kommer visas förstorat</li>
</ul>

<p><a name="editor-obfuscate"></a></p>
<h3>Censurera saker</h3>
<p>
Att censurera delar av en skärmdump är en bra idé om den innehåller information som inte är menad för andra personer, t.ex. bankuppgifter, namn, lösenord eller ansikten på bilder.<br />
Använd censursverktyget <kbd>O</kbd> precis på samma sätt som verktyget för <a href="#editor-highlight">framhävning</a>.<br />
Tillgängliga alternativ för censur är:
</p>
<ul>
<li><em>Pixelering</em>: öka pixelstorleken för det valda området</li>
<li><em>Oskärpa</em><a href="#hint-blur">*</a>: gör det markerade området oskarpt</li>
</ul>
<p><a name="hint-blur"></a></p>
<p class="hint">
* Beroende på din dators prestanda kan oskärpan göra Greenshots bildredigerare långsam. Om du känner att bildredigeraren reagerar långsamt så fort en oskärpa läggs till, kan du försöka minska värdet för <em>förhandskvalitet</em> i verktygsfältet eller minska värdet för <em>oskärpans styrka</em>.<br />
Om prestandan vid oskärpan fortfarande är för låg för dig att arbeta med, kan du istället använda pixeleringseffekten.
</p>

<p><a name="editor-crop"></a></p>
<h3>Beskära skärmdumpen</h3>
<p>
Om du enbart behöver en del av din skärmdump kan du använda beskäringsverktyget <kbd>C</kbd> för att beskära den till önskad storlek.<br />
Efter att du valt beskäringsverktyget, rita en rektangel över det område du vill ha kvar av skärmdumpen. Du kan ändra storlek på det markerade området liksom alla andra element.<br />
När du är nöjd med din markering, använd knappen <em>Verkställ</em> i verktygsfältet, eller tryck på <kbd>Enter</kbd>. Du kan avbryta beskäringen genom att klicka på knappen <em>Avbryt</em> eller genom att klicka på <kbd>Esc</kbd>.<br />
<kbd>Ctrl</kbd> + <kbd>-</kbd> beskär området omedelbart för att matcha alla befintliga element.
</p>
<p class="hint">
<em>Autobeskärning</em>: Om du behöver beskära en enfärgad kant från din skärmdump, kan du välja <em>Autobeskäring</em> från menyn <em>Redigera</em> så att Greenshot automatiskt markerar området att beskära.
</p>

<p><a name="editor-enlarge"></a>
<h3>Förstora skärmdump</h3>
<p>
För att förstora skärmdumpen, tryck på <kbd>Shift</kbd> + <kbd>+</kbd>, vilket lägger till 25 pixlar på alla fyra sidorna.
</p>

<p><a name="editor-adding-graphics"></a></p>
<h3>Lägga till grafik till en skärmdump</h3>
<p>
Du kan enkelt lägga till grafik eller bilder till dina skärmdumpar genom att dra och släppa en bildfil i redigeringsfönstret. Du kan också lägga in skärmdumpar av andra fönster genom att välja <em>Infoga fönster</em> från menyn <em>Redigera</em>. En lista med alla öppna fönster visas, där du kan välja ett att lägga till. Att infoga en bild från urklippet med <kbd>Ctrl</kbd> + <kbd>V</kbd> fungerar också.
</p>

<p><a name="editor-reuse-elements"></a></p>
<h3>Återanvända ritade element</h3>
<p>
Om du märker att du använder samma eller liknande element på de flesta av dina skärmdumpar (t.ex. ett textfält innehållandes webbläsartyp och -version, eller censur av samma element på flera olika skärmdumpar) kan du återanvända element.<br>
Välj <em>Spara objekt till fil</em> från menyn <em>Objekt</em> för att spara de nuvarande elementen för återanvändning senare. <em>Ladda in objekt från fil</em> använder samma element för en annan skärmdump.
</p>

<p><a name="editor-export"></a></p>
<h3>Exportera skärmdumpen</h3>
<p>
Efter redigering av skärmdumpen kan du exportera resultatet för olika ändamål, beroende på dina behov. Du kommer åt alla exporteringsalternativ från menyn <em>Arkiv</em>, det översta verktygsfältet eller via kortkommandon:
</p>
<ul>
<li><em>Spara</em> <kbd>Ctrl</kbd> + <kbd>S</kbd>: sparar bilden som en fil (om bilden redan sparats, annars visas dialogrutan <em>Spara som...</em></li>
<li><em>Spara som...</em> <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>S</kbd>: låter dig välja sökväg, filnamn och bildformat innan du sparar</li>
<li><em>Kopiera till urklipp</em> <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>C</kbd>: lägger en kopia av bilden i urklippet, som du kan kopiera in i andra program</li>
<li><em>Skriv ut...</em> <kbd>Ctrl</kbd> + <kbd>P</kbd>: skickar bilden till en skrivare</li>
<li><em>E-mail</em> <kbd>Ctrl</kbd> + <kbd>E</kbd>: öppnar ett nytt meddelande i din standardklient för e-post och bifogar bilden</li>
</ul>
<p class="hint">
Efter att du sparat en bild från redigeraren kan du högerklicka på statusfältet längst ner i redigerarfönstret för att antingen kopiera sökvägen till urklippet, eller öppna filens mapp i Windows Utforskaren.
</p>

<p><a name="settings"></a></p>
<h2>Inställningarna</h2>

<p><a name="settings-general"></a></p>
<h3>Allmänt</h3>
<ul>
<li><em>Språk</em>: Det språk du föredrar använda.<br />
Du kan ladda ner ytterligare språkfiler för Greenshot <a target="_blank" href="/downloads/">här</a>.</li>
<li><em>Starta Greenshot när datorn startas</em>: Starta programmet när systemet startat upp.</li>
<li><em>Kortkommandon</em>: Anpassa kortkommandona som används för att skapa skärmdumpar.</li>
<li><em>Använd systemets standardproxy</em>: Greenshot använder systemets standardinställningar för proxy för att leta efter uppdateringar, om detta alternativ är markerat.</li>
<li><em>Intervall för att leta efter uppdateringar i dagar</em>: Greenshot kan leta efter uppdateringar automatiskt. Använd denna inställning för att ställa in intervallen (i dagar) eller välj 0 för att stänga av automatiska uppdateringar.</li>
</ul>

<p><a name="settings-capture"></a></p>
<h3>Skärmdump</h3>
<ul>
<li><em>Inkludera muspekaren</em>: Om detta alternativ är markerat kommer muspekaren inkluderas i skärmdumpen. Muspekaren hanteras som ett separat element i redigeraren, så du kan ta flytta eller ta bort den senare.</li>
<li><em>Spela upp kameraljud</em>: Ljudsignal vid skärmdump</li>
<li><em>Fördröjning i millisekunder innan skärmdump</em>: Lägg till en fördröjning innan skärmdumpen faktiskt tas.</li>
<li><em>Välj fönster</em>: Istället för att skapa en skärmdump av det aktiva fönstret med en gång får du istället välja vilket fönster du vill att skärmdumpen ska bestå utav. Du kan också välja underfönster, se <a href="#capture-window">skärmdump av fönster</a>.</li>
<li>
<em>Skärmdump av Aero-fönster (enbart Windows Vista / 7)</em>: Om du använder Greenshot på Windows Vista eller Windows 7 med Aero kan du välja hur genomskinliga fönsterkanter hanteras vid skärmdumpar av fönster. Använd denna inställning för att undvika att element bakom fönstret lyser genom kanterna.
<ul>
<li><em>Automatiskt</em>: Låt Greenshot bestämma hur genomskinlighet ska hanteras.</li>
<li><em>Som visat</em>: Genomskinliga kanter visas som på skärmen.</li>
<li><em>Använd standardfärg</em>: En fast standardfärg används istället för genomskinlighet.</li>
<li><em>Använd anpassad färg</em>: Välj en anpassad färg att använda istället för genomskinlighet.</li>
<li><em>Bevara genomskinlighet</em>: Kanter behåller sin genomskinlighet, men eventuella objekt bakom fönstret döljs. (Notera: genomskinliga områden visas som ett rutmönster i redigeraren. Mönstret exporteras inte när skärmdumpen sparas. Tänk på att spara som PNG för fullt stöd för genomskinlighet.
</li>
</ul>
</li>
<li><em>Skärmdump av Internet Explorer</em>: Låter dig enkelt skapa skärmdumpar av flikar i Internet Explorer.</li>
<li><em>Matcha bildstorlek</em>: Om detta alternativ är markerat kommer redigerarfönstret automatiskt matcha storleken på skärmdumpen.</li>
</ul>

<p><a name="settings-output"></a></p>
<h3>Utdata</h3>
<ul>
<li><em>Mapp för lagring</em>: Låter dig välja destination(er) för din skärmdump när den skapats.</li>
<li><em>Inställningar för utdatafil</em>: Katalog och filnamn som ska användas när du sparar direkt eller som föreslås när du sparar (med hjälp av dialogrutan Spara som). Klicka på knappen <em>?</em> för att lära dig mer om platshållarna som kan användas som filnamnsmönster.</li>
<li><em>JPEG-inställningar</em>: Kvalitet som används när JPEG-filer sparas</li>
</ul>

<p><a name="settings-printer"></a></p>
<h3>Utskrift</h3>
<ul>
<li><em>Förminska utskriften till papprets storlek</em>: Om bildens storlek skulle överstiga papprets storlek kommer den förminskas för att passa in på sidan.</li>
<li><em>Förstora utskriften till papprets storlek</em>: Om bilden är mindre än papprets storlek kommer den skalas upp för att skrivas ut så stor som möjligt, inom papprets gränser.</li>
<li><em>Rotera utskriften till sidans orientering</em>: Bild i landskapsformat kommer roteras 90&deg; före utskrift.</li>
<li><em>Skriv ut med inverterade färger</em>: Skärmdumpar kommer inverteras innan utskrift, användbart vid t.ex. vit text på en svart bakgrund (för att spara bläck/toner).</li>
</ul>

<p><a name="settings-plugins"></a></p>
<h3>Insticksprogram</h3>
<p>
Visar en lista med installerade insticksprogram till Greenshot. Välj ett från listan och klicka på <em>Konfigurera</em> för att komma åt insticksprogrammets konfigurationsmöjligheter.
</p>

<p><a name="help"></a></p>
<h2>Vill du hjälpa till?</h2>
<p>
För nuvarande behöver vi ingen hjälp med utveckling. Däremot finns det ett antal saker du skulle kunna göra för att stödja Greenshot och utvecklingsteamet.<br />
Tack på förhand! :)
</p>
<p><a name="help-donate"></a></p>
<h3>Överväg en donation</h3>
<p>
Vi lägger ner mycket arbete på Greenshot och spenderar en hel del tid till att tillhandahålla ett bra program helt gratis och med öppen källkod. Om du tycker att programmet gör dig mer produktiv, om det sparar dig (eller ditt företag) en massa tid och pengar, eller om du helt enkelt bara tycker om Greenshot och tanken på mjukvara med öppen källkod: överväg att belöna vårt arbete genom att donera.<br />
Ta en titt på vår hemsida för få reda på hur du kan stödja Greenshots utvecklingsteam:<br />
<a target="_blank" href="/support/">http://getgreenshot.org/support/</a>
</p>

<a name="help-spread"></a>
<h3>Sprid ordet</h3>
<p>
Om du gillar Greenshot, låt folk veta: berätta för dina vänner och kollegor om Greenshot.<br />
Dina följare också! :)<br />
Betygsätt Greenshort i mjukvaruportaler eller länka till vår hemsida från din blogg eller hemsida.
</p>

<p> <a name="help-translate"></a></p>
<h3>Skicka en översättning</h3>
<p>
Är inte Greenshot tillgängligt på ditt föredragna språk? Om du känner dig lämplig för att översätta en mjukvara, är du mer än välkommen att hjälpa till.<br />
Om du är en registrerad användare på sourceforge.net kan du skicka översättningar till vår <a target="_blank" href="https://sourceforge.net/tracker/?group_id=191585&atid=1368020">översättningsspårare</a>.<br />
Se till att det inte finns någon befintlig översättning för ditt språk på vår <a target="_blank" href="/downloads/">nedladdningssida</a>. Kolla även vår <a href="https://sourceforge.net/tracker/?group_id=191585&atid=1368020">översättningsspårare</a>, det kan vara en översättning på gång, eller åtminstone i diskussion.<br />
Observera att vi endast tillhandahåller en översättning på vår nedladdningssida om den har skickats via ditt sourceforge.net-användarkonto. Eftersom vi med största sannolikhet inte kan förstå din översättning, den är bra för andra sourceforge-användare att kunna kontakta dig om förbättringar vid t.ex. en ny Greenshot-version.
</p>
