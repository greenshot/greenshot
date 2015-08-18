---
layout: page
status: publish
published: true
title: Guida di Greenshot
author:
  display_name: greenshot
  login: admin
  email: greenshot-developers@lists.sourceforge.net
  url: http://getgreenshot.org/
author_login: admin
author_email: greenshot-developers@lists.sourceforge.net
author_url: http://getgreenshot.org/
wordpress_id: 388
wordpress_url: http://getgreenshot.org/
date: !binary |-
  MjAxMi0wNC0wOSAwODozMjozMiArMDIwMA==
date_gmt: !binary |-
  MjAxMi0wNC0wOSAwNjozMjozMiArMDIwMA==
categories: []
tags: []
comments: []
---
<p><small>Versione 1.0<!-- - English translation of help content by YOUR_NAME--></small></p>
<h2>Contenuti</h2>
<ol>
<li><a href="#screenshot">Creazione immagine dello schermo</a></li>
<ol>
<li><a href="#capture-region">Cattura regione</a></li>
<li><a href="#capture-last-region">Cattura ultima regione</a></li>
<li><a href="#capture-window">Cattura finestra</a></li>
<li><a href="#capture-fullscreen">Cattura schermo intero</a></li>
<li><a href="#capture-ie">Cattura Internet Explorer</a></li>
</ol>
<li><a href="#editor">Uso della Gestione Immagini</a></li>
<ol>
<li><a href="#editor-shapes">Disegnare forme</a></li>
<li><a href="#editor-text">Aggiungere testo</a></li>
<li><a href="#editor-highlight">Evidenziare qualcosa</a></li>
<li><a href="#editor-obfuscate">Offuscare qualcosa</a></li>
<li><a href="#editor-effects">Aggiungere effetti</a></li>
<li><a href="#editor-crop">Ritagliare l'immagine</a></li>
<li><a href="#editor-adding-graphics">Aggiungere elementi grafici all'immagine</a></li>
<li><a href="#editor-reuse-elements">Riutilizzare gli elementi disegnati</a></li>
<li><a href="#editor-export">Esportare l'immagine</a></li>
</ol>
<li><a href="#settings">La pagina delle Impostazioni</a></li>
<ol>
<li><a href="#settings-general">Impostazioni Generali</a></li>
<li><a href="#settings-capture">Impostazioni di Cattura</a></li>
<li><a href="#settings-output">Impostazioni di Emissione</a></li>
<li><a href="#settings-destination">Impostazioni di Destinazione</a></li>
<li><a href="#settings-printer">Impostazioni Stampante</a></li>
<li><a href="#settings-expert">Impostazioni per utenti Esperti</a></li>
</ol>
<li><a href="#help">Vuoi aiutarci?</a></li>
<ol>
<li><a href="#help-donate">Considera una donazione</a></li>
<li><a href="#help-spread">Spargi la parola</a></li>
<li><a href="#help-translate">Invia una traduzione</a></li>
</ol>
</ol>
<p>	<a name="screenshot"></a></p>
<h2>Creazione immagine dello schermo</h2>
<p>
		L'immagine può essere creata utilizzando il tasto <kbd>Stamp</kbd> della tastiera,<br />
		oppure cliccando il tasto destro del mouse sull'icona di Greenshot nella barra.<br><br />
		Ci sono varie opzioni per creare un'immagine:
	</p>
<p>	<a name="capture-region"></a></p>
<h3>Cattura regione <kbd>Stamp</kbd></h3>
<p>
		Il metodo cattura regione consente di selezionare una parte dello schermo da "fotografare".<br><br />
		Dopo aver avviato il metodo regione, apparirà un mirino sulla posizione del mouse sullo<br />
		schermo. Cliccare e tenere premuto dove si vuole impostare un angolo della regione da<br />
		fotografare. Tenendo premuto il pulsante del mouse, muovere il mouse fino a definire il<br />
		rettangolo da fotografare. Rilasciare quindi il pulsante quando il rettangolo verde avrà<br />
		coperto l'area da catturare nell'immagine.
	</p>
<p class="hint">
		Si può usare il tasto <kbd>Spazio</kbd> per cambiare da metodo regione a metodo<br />
		<a href="#capture-window">finestra</a>.
	</p>
<p class="hint">
		Se si vuol catturare precisamente un'area, potrebbe risultare più facile selezionare<br />
		un'area più grande e quindi <a href="#editor-crop">ritagliare</a> l'immagine in<br />
		seguito, utilizzando la Gestione Immagini di Greenshot.
	</p>
<p>	<a name="capture-last-region"></a></p>
<h3>Cattura ultima regione <kbd>Maiusc</kbd> + <kbd>Stamp</kbd></h3>
<p>
		Usando questa opzione, se avete già eseguito un cattura <a href="#capture-region">regione</a> o <a href="#capture-window">finestra</a>,<br />
		si può ricatturare automaticamente la stessa regione.
	</p>
<p>	<a name="capture-window"></a></p>
<h3>Cattura finestra <kbd>Alt</kbd> + <kbd>Stamp</kbd></h3>
<p>
		Crea un'immagine della finestra che è attiva in quel momento.
	</p>
<p class="hint">
		La <a href="#settings">pagina delle impostazioni</a> offre una possibilità per non catturare<br />
		direttamente la finestra attiva, consentendo quindi di sceglierne una interattivamente.<br />
		Se si seleziona questa opzione, la finestra può essere scelta cliccandovi (come nel metodo<br />
		<a href="#capture-region">regione</a>, Greenshot evidenzierà l'area che verrà catturata).<br />
		<br>Se si vuol catturare una finestra figlia (es: una browser<br />
		viewport (senza barra strumenti, ecc...) o un singolo frame di una pagina web che usa i framesets)<br />
		si può puntare il cursore del mouse sulla finestra e premere il tasto <kbd>PgDown</kbd>. Dopo di questo, sarà<br />
		possibile selezionare elementi da catturare nella finestra figlia.
	</p>
<p>	<a name="capture-fullscreen"></a></p>
<h3>Cattura schermo intero <kbd>Ctrl</kbd> + <kbd>Stamp</kbd></h3>
<p>
		Crea un'immagine dell'intero schermo.<br />
	<a name="capture-ie"></a></p>
<h3>Cattura Internet Explorer <kbd>Ctrl</kbd> + <kbd>Maiusc</kbd> + <kbd>Stamp</kbd></h3>
<p>
		Crea facilmente un'immagine della pagina web attiva in quel momento su Internet Explorer.<br />
		Si può usare il menu sensibile al contesto di Greenshot per selezionare la tab di Internet Explorer da catturare, oppure premere<br />
		<kbd>Crtl</kbd> + <kbd>Maiusc</kbd> + <kbd>Stamp</kbd> per catturare la tab attiva.
	</p>
<p>	<a name="editor"></a></p>
<h2>Uso della Gestione Immagini</h2>
<p>
		Greenshot fornisce anche una pratica gestione delle immagini, che include degli utili strumenti<br />
		per aggiungere note e forme alle immagini. Essa permette inoltre di evidenziare o<br />
		offuscare parti dell'immagine.
	</p>
<p class="hint">
		La Gestioni Immagini di Greenshot non è solo per le immagini catturate. Si può usare<br />
		anche per aprire e modificare immagini da file o da Appunti. E' sufficiente premere il tasto destro<br />
		sull'icona di Greenshot nella barra, e selezionare rispettivamente <em>Apri immagine da file</em><br />
		o <em>Apri immagine da Appunti</em>.
	</p>
<p class="hint">
		Come default, la gestione immagini verrà aperta ogniqualvolta un'immagine viene catturata.<br />
		Se non si vuole passare per la gestione immagini, si può disabilitare questo funzionamento<br />
		nella <a href="#settings">pagina delle impostazioni</a>.
	</p>
<p>	<a name="editor-shapes"></a></p>
<h3>Disegnare forme</h3>
<p>
		Selezionare uno degli strumenti di disegno dalla barra degli strumenti sul lato sinistro<br />
		della gestione immagini o dal menù <em>Oggetti</em>. Per facilitarne la selezione, ciascun<br />
		strumento è assegnato ad un tasto.<br><br />
		Le forme disponibili sono: rettangolo <kbd>R</kbd>, ellisse <kbd>E</kbd>, linea <kbd>L</kbd><br />
		e freccia <kbd>A</kbd>.<br><br />
		Cliccare, tenendo premuto il pulsante del mouse e trascinare per definire la posizione e la dimensione della forma.<br />
		Completata la definizione, rilasciare il pulsante del mouse.
	</p>
<p>
		Le forme possono essere mosse e ridimensionate facilmente, previa selezione mediante lo strumento<br />
		<kbd>ESC</kbd> disponibile nella barra a sinistra.<br>Per ciascun tipo di elemento c'è un gruppo di<br />
		opzioni specifiche per cambiarne l'aspetto (es: spessore linea,<br />
		colore linea, colore di riempimento). Si possono modificare le opzioni di un elemento esistente, previa selezione,<br />
		e anche quelle di nuovi elementi da disegnare, previa selezione dello strumento di disegno.
	</p>
<p class="hint">
		Si possono inoltre selezionare più elementi per una modifica simultanea. Per selezionare più elementi,<br />
		tenere premuto il tasto <kbd>Maiusc</kbd> mentre si clicca sugli elementi.
	</p>
<p>	<a name="editor-text"></a></p>
<h3>Aggiungere testo</h3>
<p>
		L'uso dello strumento di testo <kbd>T</kbd> è simile all'uso degli strumenti di disegno<br />
		<a href="#editor-shapes">forme</a>. E' sufficiente disegnare l'elemento di testo delle dimensioni desiderate,<br />
		e quindi digitare il testo.<br><br />
		Per modificare il testo di un elemento esistente, premere il doppio click sull'elemento.
	</p>
<p>	<a name="editor-highlight"></a></p>
<h3>Evidenziare qualcosa</h3>
<p>
		Dopo aver selezionato lo strumento di evidenziazione <kbd>H</kbd>, definire l'area da evidenziare esattamente<br />
		come si volesse disegnare una <a href="#editor-shapes">forma</a>.<br><br />
		Ci sono varie opzioni per evidenziare, esse possono essere selezionate cliccando il pulsante<br />
		più in alto a sinistra nella barra degli strumenti:
	</p>
<ul>
<li><em>Evidenzia il testo</em>: evidenzia un'area applicando un colore brillante ad essa, come un<br />
			pennarello evidenziatore</li>
<li><em>Evidenzia l'area</em>: sfuoca<a href="#hint-blur">*</a> e scurisce tutto all'esterno dell'area selezionata</li>
<li><em>Scala di grigi</em>: tutto ciò che è al di fuori dell'area selezionata viene trasformato in scala di grigi</li>
<li><em>Ingrandisci</em>: l'area selezionata verrà visualizzata come ingrandita da una lente</li>
</ul>
<p>	<a name="editor-obfuscate"></a></p>
<h3>Offuscare qualcosa</h3>
<p>
		Offuscare parti di un'immagine può essere una buona idea se essa contiene dati privati che non devono essere<br />
		visti da altre persone, per esempio dati conto bancario, nomi, parole d'ordine o volti di persone.<br><br />
		Usare lo strumento di offuscamento <kbd>O</kbd> esattamente come lo strumento di <a href="#editor-highlight">evidenziazione</a>.<br><br />
		Le opzioni disponibili per l'offuscamento, sono:
	</p>
<ul>
<li><em>Offuscare/pixelize</em>: aumenta le dimensioni dei pixel nell'area selezionata</li>
<li><em>Sfuma</em><a href="#hint-blur">*</a>: sfuma e sfuoca l'area selezionata</li>
</ul>
<p>	<a name="hint-blur"></a></p>
<p class="hint">
		* A seconda delle prestazioni del proprio PC, applicare un effetto di sfumatura potrebbe rallentare la Gestione<br />
		Immagini di Greenshot. Se si vede che la Gestione Immagini risponde lentamente subito dopo aver eseguito una sfumatura,<br />
		è utile provare a ridurre il valore di <em>Qualità anteprima</em> nella barra strumenti di offuscamento,<br />
		o a diminuire il valore di <em>Raggio sfumatura</em>.<br><br />
		Se le prestazioni della sfumatura sono ancora deludenti per poterci lavorare, si consiglia si usare invece<br />
		l'effetto Offusca/pixelize.
	</p>
<p>	<a name="editor-effects"></a>	</p>
<h3>Aggiungere effetti</h3>
<p>
		Si possono aggiungere vari effetti all'immagine. Per esempio si può aggiungere un bordo, una<br />
		ombra, oppure un bordo strappato, in modo da separare l'immagine da altri elementi contenuti<br />
		nel documento. Gli effetti Scala dei Grigi e Negativo sono utili principalmente prima<br />
		di stampare, in modo da risparmiare inchiostro o toner nei casi di immagini molto colorate o scure.
	</p>
<p>	<a name="editor-crop"></a></p>
<h3>Ritagliare l'immagine</h3>
<p>
		Per ricavare solo una parte dell'immagine catturata, si può usare lo strumento di ritaglio <kbd>C</kbd><br />
		per ritagliare l'area desiderata.<br><br />
		Dopo aver selezionato lo strumento di ritaglio, disegnare un rettangolo per l'area che si vuole mantenere.<br />
		Come per gli atri elementi, si possono facilmente modificare le dimensioni dell'area selezionata.<br><br />
		Dopo aver impostato correttamente la selezione dell'area, premere il pulsante di conferma della barra strumenti<br />
		oppure premere il tasto <kbd>Invio</kbd>. Si può annullare l'azione di ritaglio, cliccando il pulsante di cancellazione o premendo<br />
		<kbd>ESC</kbd>.
	</p>
<p class="hint">
		<em>Ritaglia Automaticamente</em>: Se si ha la necessità di ritagliare un pezzo dell'immagine lungo i bordi di uno sfondo con colore compatto,<br />
		è sufficiente scegliere <em>Ritaglia Automaticamente</em> dal menù <em>Modifica</em> e Greenshot automaticamente<br />
		selezionerà l'area di ritaglio.
	</p>
<p>	<a name="editor-adding-graphics"></a></p>
<h3>Aggiunta elementi grafici all'immagine</h3>
<p>
		Si possono facilmente aggiungere degli elementi grafici o delle altre immagini alla immagine in lavorazione, è sufficiente trascinare un file di immagine<br />
		all'interno della finestra di gestione immagini. Inoltre, selezionando <em>Inserisci finestra</em> dal menù <em>Modifica</em>, si possono inserire<br />
		immagini prese da altre finestre. In questo caso, appare una lista con tutte le finestre aperte, consentendo quindi di scegliere quella che si<br />
		vuole inserire.
	</p>
<p>	<a name="editor-reuse-elements"></a></p>
<h3>Ri-utilizzare elementi disegnati</h3>
<p>
		Se ci si ritrova a utilizzare lo stesso o simile elemento nella maggior parte delle immagini,<br />
		(es: campo di testo contenente tipo browser e versione, oppure offuscamento dello stesso elemento<br />
		su più immagini), è possibile riutilizzare gli elementi in modo semplice.<br><br />
		Selezionare <em>Salva oggetti su file</em> dal menù <em>Oggetti</em> per salvare di elementi correnti<br />
		per poterli riutilizzare poi. <em>Carica oggetti da file</em> viene invece usato per applicare su un'altra immagine<br />
		gli elementi salvati in precedenza.
	</p>
<p>	<a name="editor-export"></a></p>
<h3>Esportare l'immagine</h3>
<p>
		Dopo aver modificato l'immagine, si può esportare il risultato per vari scopi, a seconda delle necessità.<br />
		Si può accedere a tutte le opzioni di esportazione mediante il menù <em>File</em>,<br />
		sulla barra principale, o per mezzo delle seguenti scorciatoie:
	</p>
<ul>
<li><em>Salva</em> <kbd>Ctrl</kbd> + <kbd>S</kbd>: salva l'immagine su un file (se l'immagine è già stata salvata, altrimenti emette la finestra di <em>Salva come...</em>)</li>
<li><em>Salva come...</em> <kbd>Ctrl</kbd> + <kbd>Maiusc</kbd> + <kbd>S</kbd>: permette di scegliere la destinazione, il nome file e il formato immagine per il file da salvare</li>
<li><em>Copia immagine sugli appunti</em> <kbd>Ctrl</kbd> + <kbd>Maiusc</kbd> + <kbd>C</kbd>: mette una copia dell'immagine sugli appunti, consentendo poi di incollarla dentro altri programmi</li>
<li><em>Stampa...</em> <kbd>Ctrl</kbd> + <kbd>P</kbd>: invia l'immagine a una stampante</li>
<li><em>E-Mail</em> <kbd>Ctrl</kbd> + <kbd>E</kbd>: apre un nuovo messaggio sul programma di e-mail di default, aggiungendo l'immagine come allegato</li>
</ul>
<p class="hint">
		Dopo aver salvato un'immagine dalla gestione, cliccando con il tasto destro del mouse sulla barra di stato in basso sulla finestra<br />
		della gestione immagini, è possibile copiare il percorso sugli appunti, oppure aprire la cartella di destinazione con la gestione risorse.
	</p>
<p>	<a name="settings"></a></p>
<h2>Le Preferenze</h2>
<p>	<a name="settings-general"></a></p>
<h3>Impostazioni Generali</h3>
<ul>
<li><em>Lingua</em>: La lingua che si preferisce usare.<br><br />
			Si possono scaricare i file per le lingue aggiuntive di Greenshot <a href="#">qui</a>. </li>
<li><em>Lancia Greenshot all'avvio</em>: Avvia il programma in automatico all'accensione del sistema.</li>
<li><em>Scorciatoie di tastiera</em>: Personalizza le scorciatoie (hotkeys) da usare per catturare le immagini.</li>
<li><em>Usa il proxy di default del sistema</em>: Se selezionato, Greenshot usa il proxy di default del sistema per controllare se ci sono aggiornamenti.</li>
<li><em>Intervallo di controllo aggiornamento, in giorni</em>: Greenshot può controllare automaticamente se ci sono aggiornamenti. Questo parametro può essere<br />
			utilizzato per specificare l'intervallo (in giorni); per disabilitare il controllo aggiornamento si può usare il valore 0.</li>
</ul>
<p>	<a name="settings-capture"></a></p>
<h3>Impostazioni di Cattura</h3>
<ul>
<li><em>Cattura puntatore mouse</em>: Se selezionato, il puntatore del mouse verrà catturato. Il puntatore viene gestito come un elemento separato, in modo che possa essere spostato o rimosso in seguito.</li>
<li><em>Emetti suono fotocamera</em>: Attiva il riscontro udibile dell'azione di cattura (suono dello scatto fotografico).</li>
<li><em>Millisecondi di attesa prima di catturare</em>: Utile per aggiungere un tempo di ritardo prima dell'effettiva cattura dello schermo.</li>
<li><em>Usa la modalità di cattura via finestra interattiva</em>: Invece di catturare direttamente la finestra attiva, la modalità interattiva<br />
			consente di selezionare la finestra da catturare. E' inoltre possibile catturare le finestre figlie, vedi <a href="#capture-window">Cattura finestra</a>.</li>
<li>
			<em>Cattura in stile Aero (so Windows Vista / 7)</em>: Se si sta usando Greenshot su Windows Vista or Windows 7 con lo stile di visualizzazione Aero abilitato, è possibile<br />
			scegliere come devono essere gestiti i bordi della finestra trasparente quando vengono create le immagini in modalità finestra. Questa impostazione è da usare per evitare<br />
			la cattura di elementi dello sfondo che appaiono attraverso i bordi trasparenti.</p>
<ul>
<li><em>Automaticamente</em>: Greenshot deciderà come gestire la trasparenza.</li>
<li><em>Come visualizzata</em>: I bordi trasparenti verranno catturati come visualizzati sullo schermo.</li>
<li><em>Usa i colori di default</em>: Al posto della trasparenza verrà applicato un colore compatto di default.</li>
<li><em>Usa colori personalizzati</em>: Si può scegliere il colore che sarà applicato al posto della trasparenza.</li>
<li><em>Conserva la trasparenza</em>: I bordi verranno catturati conservando la trasparenza, senza però catturare gli elementi che potrebbero essere sullo sfondo. (Nota: le aree<br />
					trasparenti verrano visualizzate usando un modello selezionato nella gestione immagini. Il modello non verrà esportato se si salverà l'immagine su file. Ricordarsi di salvare<br />
					il file come PNG se si vuole conservance il pieno supporto della trasparenza.)</li>
</ul>
</li>
<li><em>Cattura Internet Explorer</em>: Abilita la comoda cattura delle pagine web utilizzando Internet Explorer.</li>
<li><em>Adatta finestra Gestione a dimensioni di cattura</em>: Se selezionata, la finestra della Gestione immagini verrà automaticamente ridimensionata in base alla dimensione dell'immagine catturata.</li>
</ul>
<p>	<a name="settings-output"></a></p>
<h3>Impostazioni di Emissione</h3>
<ul>
<li><em>Destinazione dell'immagine</em>: Consente di scegliere la destinazione/i automatiche delle immagini subito dopo l'azione di cattura.</li>
<li><em>Impostazioni Preferite per l'Emissione File</em>: Cartella e nome file da usare quando si salva automaticamente, o da suggerire quando si salva (usando la finestra "Salva come"). Cliccare il pulsante <em>?</em> per sapere di più sulle variabili che possono essere usate nel modello del nome file.</li>
<li><em>Impostazioni JPEG</em>: Qualità da usare quando si salvano file JPEG.</li>
</ul>
<p>	<a name="settings-destination"></a></p>
<h3>Impostazioni di Destinazione</h3>
<p>
		Come default, Greenshot consente all'utente di scegliere dinamicamente una destinazione dopo aver creato l'immagine,<br />
		visualizzando un piccolo menù con le varie destinazioni selezionabili. Se non si vuole o se è necessario cambiare destinazione<br />
		istantaneamente, si può configurare Greenshot in modo che esporti direttamente l'immagine verso uno o più destinazioni, senza<br />
		visualizzare la selezione delle destinazioni.<br/><br />
		Nota: come per <a href="#editor-export">l'esportazione dalla finestra di Gestione Immagini, le destinazioni disponibili variano<br />
		a seconda dei componenti aggiuntivi (plugins) che sono stati installati con Greenshot.</a>
	</p>
<p>	<a name="settings-printer"></a></p>
<h3>Impostazioni Stampante</h3>
<ul>
<li><em>Riduci alle dimensioni pagina</em>: Se l'immagine eccede le dimensioni della pagina, essa verrà ridotta e adattata alle dimensioni della pagina.</li>
<li><em>Ingrandisci fino alle dimensioni pagina</em>: Se l'immagine è più piccola delle dimensioni della pagina, essa verrà ingrandita per stamparla più grande possibile senza superare le dimensioni della pagina.</li>
<li><em>Ruota a seconda dell'orientamento pagina</em>: Ruoterà l'immagine in formato orizzontale di 90° per la stampa.</li>
<li><em>Centra nella pagina</em>: L'immagine verrà stampata al centro della pagina.</li>
<li><em>Stampa data / ora sul piede della pagina</em>: La data e l'ora di stampa verranno stampati sul piede della pagina.</li>
<li><em>Stampa con colori inverititi (negativo)</em>: Trasformerà l'immagine in negativo prima di stamparla, utile per esempio quando si stampa un'immagine con testo bianco su sfondo nero (per risparmiare toner o inchiostro).</li>
<li><em>Visualizza scelta opzioni di stampa ogni volta che si stampa un'immagine</em>: Permette di scegliere se visualizzare o meno la finestra di scelta opzioni per le stampe successive alla prima.</li>
</ul>
</ul>
<p>	<a name="settings-plugins"></a></p>
<h3>Impostazioni Componenti Aggiuntivi</h3>
<p>
		Visualizza la lista dei componenti aggiuntivi di Greenshot che sono attualmente installati. La configurazione del singolo componente aggiuntivo è disponibile selezionando<br />
		il componente dalla lista e quindi cliccando su <em>Configura</em>.
	</p>
<p><a name="settings-expert"></a></p>
<h3>Impostazioni per utenti Esperti</h3>
<p>
		Attenzione! E' meglio non toccare queste impostazioni se non si conosce esattamente ciò che si sta facendo, poichè queste impostazioni possono provocare dei comportamenti inaspettati.
	</p>
<p>	<a name="help"></a></p>
<h2>Desideri aiutarci?</h2>
<p>
		Attualmente non abbiamo bisogno di aiuto per lo sviluppo. Tuttavia, ci sono molte cose che puoi fare per<br />
		supportare Greenshot e il team di sviluppo.<br><br />
		Grazie anticipatamente :)
	</p>
<p>	<a name="help-donate"></a></p>
<h3>Considera una donazione</h3>
<p>
		Stiamo lavorando molto su Greenshot e stiamo spendendo molto tempo per fornire<br />
		un buon prodotto software gratuito e open source. Se ti sei reso conto che Greenshot<br />
		ti ha reso più produttivo, e se fa risparmiare a te (o alla tua società)<br />
		molto tempo e denaro, o se semplicemente ti piace Greenshot e l'idea<br />
		di software open source: per cortesia, considera di onorare i nostri sforzi con una donazione.<br><br />
		Per cortesia dai un'occhiata alla nostra home page per vedere come puoi aiutare il team di sviluppo di Greenshot:<br><br />
		<a href="/support/">http://getgreenshot.org/support/</a>
	</p>
<p>	<a name="help-spread"></a></p>
<h3>Spargi la parola</h3>
<p>
		Se ti piace Greenshot, fallo sapere anche agli altri: racconta ai tuoi amici di Greenshot.<br />
		Anche loro, a loro volta :)<br><br />
		Commenta positivamente Greenshot sui portali di software, oppure metti un link sulla tua home page, blog o sito web.
	</p>
<p>	<a name="help-translate"></a></p>
<h3>Invia una traduzione</h3>
<p>
		Greenshot non è disponibile nella tua lingua preferita? Se ti senti in grado di tradurre un pezzo di software,<br />
		sei più che benvenuto.<br />
		Se sei un utente registrato su sourceforge.net, puoi inviare le traduzioni al nostro<br />
		<a href="https://sourceforge.net/tracker/?group_id=191585&atid=1368020">translations tracker</a>.<br><br />
		Prima di farlo, assicurati che non esista già la traduzione sulla nostra<br />
		<a href="http://sourceforge.net/projects/greenshot/files/">pagina di download</a>. Controlla anche il nostro <a href="https://sourceforge.net/tracker/?group_id=191585&atid=1368020">translations tracker</a>,<br />
		ci potrebbe essere una traduzione in lavorazione, o almeno in discussione.<br><br />
		Ti preghiamo di notare che forniremo una traduzione della nostra pagina di download solo se è stata inviata mediante<br />
		il tuo conto utente su sourceforge.net. Visto che molto probabilmente non siamo in grado di capire la traduzione, è opportuno<br />
		che gli altri utenti di sourceforge possano essere in grado di contattarti per revisioni o miglioramenti<br />
		in caso di nuove versioni di Greenshot.
	</p>
