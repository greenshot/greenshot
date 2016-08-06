---
layout: page
status: publish
published: true
title: Aide Greenshot 
permalink: /help/fr-fr/
categories: []
tags: []
comments: []
---
<div class="pull-right">{% include help-nav.html %}</div>

<small>Version 0.8<!-- - English translation of help content by YOUR_NAME--></small>

<h2>Contents</h2>
<ol>
<li><a href="#screenshot">Capturer un &eacute;cran</a></li>
<ol>
<li><a href="#capture-region">Capturer une zone de l&rsquo;&eacute;cran</a></li>
<li><a href="#capture-last-region">Capturer la dern&egrave;re zone</a></li>
<li><a href="#capture-window">Capturer une fen&ecirc;tre</a></li>
<li><a href="#capture-fullscreen">Capturer l&eacute;cran</a></li>
</ol>

<li><a href="#editor">Utilisation de l&rsquo;&eacute;diteur d&rsquo;image</a></li>
<ol>
<li><a href="#editor-shapes">Dessiner des formes</a></li>
<li><a href="#editor-text">Ajouter un texte</a></li>
<li><a href="#editor-highlight">Accentuer des &eacute;l&eacute;ments</a></li>
<li><a href="#editor-obfuscate">Obscurcir des &eacute;l&eacute;ments</a></li>
<li><a href="#editor-crop">Rogner la capture d&rsquo;&eacute;cran</a></li>
<li><a href="#editor-reuse-elements">R&eacute;utiliser des &eacute;l&eacute;ments dessin&eacute;s</a></li>
<li><a href="#editor-export">Exporter la capture d&rsquo;&eacute;cran</a></li>
</ol>
<li><a href="#settings">Param&egrave;trages</a></li>
<ol>
<li><a href="#settings-general">Param&egrave;tres gé&eacute;né&eacute;raux</a></li>
<li><a href="#settings-output">Param&egrave;tres de sortie</a></li>
<li><a href="#settings-printer">Param&egrave;tres d&rsquo;impression</a></li>
</ol>
<li><a href="#help">Souhaitez&#45;vous participer au d&eacute;veloppement ?</a></li>
<ol>
<li><a href="#help-donate">Faire une donation</a></li>
<li><a href="#help-spread">Diffuser l&rsquo;information</a></li>
<li><a href="#help-translate">soumettre une traduction</a></li>
</ol>
</ol>

<a name="screenshot"></a>
<h2>Capturer un &eacute;cran</h2>
<p>
Vous pouvez cr&eacute;er une capture d&rsquo;&eacute;cran soit en utilisant la touche <kbd><b>Impr &Eacute;cran &#40;Print&#41;</b></kbd> du clavier
ou en effectuant un clic droit sur l&rsquo;ic&ocirc;ne Greenshot de la zone de notification.<br>
Il existe diff&eacute;rentes possibilit&eacute;s pour cr&eacute;er une capture d&rsquo;&eacute;cran:
</p>

<a name="capture-region"></a>
<h3>Capturer une zone de l&rsquo;&eacute;cran <kbd>Impr &Eacute;cran &#40;Print&#41;</kbd></h3>
<p>
Le mode capture de zone vous permet de s&eacute;lectionner la partie d&rsquo;&eacute;cran que vous souhaitez capturer.<br>
Apr&egrave;s avoir s&eacute;lectionn&eacute; le mode capture de zone, une croix apparait qui pointe la position de la souris
sur l&rsquo;&eacute;cran. Cliquer et maintenez le bouton de la souris enfonc&eacute; &agrave; l&rsquo;endroit<br>o&ugrave; vous 
voulez commencer votre s&eacute;lection. Tout en maintenant le bouton de la souris enfonc&eacute;, d&eacute;placez la souris
pour d&eacute;finir le rectangle &agrave; capturer. Lorsque le rectangle vert<br>recouvre toute la surface que vous souhaitez
capturer, relachez le bouton de la souris.
</p>
<p class="hint">
Vous pouvez utiliser la touche <kbd>Espace</kbd> pour passer du mode capture de zone au mode capture de  
<a href="#capture-window">fen&ecirc;tre</a>.
</p>
<p class="hint">
Si vous souhaitez capturer une zone bien pr&eacute;cise, il peut s&rsquo;av&eacute;rer plus simple  de s&eacute;lectionner une
zone l&eacute;g&egrave;rement plus grande puis de <a href="#editor-crop">rogner</a> plus tard la 
capture d&rsquo;&eacute;cran<br>en utilisant l&rsquo;&eacute;diteur d&rsquo;image de Greenshot.
</p>

<a name="capture-last-region"></a>
<h3>Capture de la derni&egrave;re zone <kbd>Maj &#40;Shift&#41;</kbd> + <kbd>Impr &Eacute;cran &#40;Print&#41;</kbd></h3>
<p>
Si vous avez effectu&eacute; une capture de <a href="#capture-region">zone</a> ou de <a href="#capture-window">fen&ecirc;tre</a> pr&eacute;c&eacute;demment,
vous pouvez capturer &agrave; nouveau cette zone en utilisant cette option. 
</p>

<a name="capture-window"></a>
<h3>Capture d&rsquo;une fen&ecirc;tre <kbd>Alt</kbd> + <kbd>Impr &Eacute;cran &#40;Print&#41;</kbd></h3>
<p>
Cette option cr&eacute;e une capture d&rsquo;&eacute;cran de la fen&ecirc;tre qui est actuellement active.
Si cette option est active, une fenêtre peut &ecirc;tre s&eacute;lectionn&eacute;e en cliquant dessus &#40;d'une fa&ccedil;on analogue au 
<a href="#capture-region">mode zone</a>, Greenshot mettra en surbrillance la zone qui 
sera captur&eacute;e&#41;.<br>Si vous souhaitez capturer une fenêtre adjacente &#40; par ex. une fen&ecirc;tre d&rsquo;affichage de navigateur
&#40;sans la barre d&rsquo;outils etc.&#41; ou un seul cadre d&rsquo;une page web utilisant des &laquo;Frames&raquo;&#41;<br> 
pointer le curseur de la souris sur la fen&ecirc;tre et frapper sur la touche <kbd>Page suivante &#40;PgDown&#41;</kbd>. Apr&egrave;s
cette proc&eacute;dure, vous pouvez s&eacute;lectionner les &eacute;l&eacute;ments compl&eacute;mentaires<br>dans la fen&ecirc;tre que vous souhaitez capturer.
</p>

<a name="capture-fullscreen"></a>
<h3>Capturer un &eacute;cran complet <kbd>Ctrl &#40;Control&#41;</kbd> + <kbd>Impr &Eacute;cran &#40;Print&#41;</kbd></h3>
<p>
Cr&eacute;e une capture d&rsquo;&eacute;cran d'un &eacute;cran complet.
</p>

<a name="editor"></a>
<h2>Utilisation de l&rsquo;&eacute;diteur d&rsquo;image</h2>
<p>
Greenshot inclut un &eacute;diteur d&rsquo;images simple, &eacute;quip&eacute; d&rsquo;un jeu de fonctionnalit&eacute;s commodes
pour ajouter des annotations ou des formes &agrave; la capture d&rsquo;&eacute;cran.<br>Il permet &eacute;galement de mettre en valeur ou 
d&rsquo;att&eacute;nuer des parties de la capture.
</p>
<p class="hint">
L&rsquo;utilisation de l&rsquo;&eacute;diteur d&rsquo;image de Greenshot n&rsquo;est pas limit&eacute; aux captures d&rsquo;&eacute;cran. On peut &eacute;galement 
ouvrir les images d'un fichier ou du presse&#45;papier pour les &eacute;diter.<br>Faire simplement un click droit 
sur l'ic&ocirc;ne de Greenshot dans la zone de notification et s&eacute;lectionner respectivement <em>Ouvrir un fichier image</em> 
ou <em>Ouvrir une image &agrave; partir du presse&#45;papier</em>.
</p>
<p class="hint">
Par d&eacute;faut, l&rsquo;&eacute;diteur d&rsquo;image sera ouvert d&egrave;s la capture d&rsquo;une image.
Si l&rsquo;on ne souhaite pas utiliser l&rsquo;&eacute;diteur d&rsquo;image, il est possible d&rsquo;inhiber cette
fonction dans les <a href="#settings">param&egrave;tres</a>.
</p>


<a name="editor-shapes"></a>
<h3>Dessiner des formes</h3>
<p>
S&eacute;lectionner l&rsquo;un des outils de dessin de forme dans la barre d&rsquo;outils sur le c&ocirc;t&eacute; gauche
de l&rsquo;&eacute;diteur d&rsquo;image ou dans le menu <em>Objet</em>. Un raccourci est &eacute;galement associ&eacute;
&agrave; chaque outil si l&rsquo;on pr&eacute;f&egrave;re cette approche.<br>
Les outils disponibles sont : le rectange <kbd>R</kbd>, l&rsquo;ellipse <kbd>E</kbd>, la ligne <kbd>L</kbd>
et la fl&egrave;che <kbd>A</kbd>.<br>
Cliquer et maintenir le bouton de la souris enfonc&eacute; et la faire glisser pour d&eacute;finir la position et la dimension de la forme. 
Relacher le bouton de la souris lorsque votre s&eacute;lection est correcte.
</p>
<p>
On peut d&eacute;placer et redimensionner les formes existantes apr&egrave;s avoir s&eacute;lectionn&eacute; l&rsquo;outil s&eacute;lection 
<kbd>ESC</kbd> de la barre d&rsquo;outils.<br> Un jeu specifique d'options est disponible pour chaque type d&rsquo;&eacute;l&eacute;ments 
qui permettent de changer l&rsquo;aspect de l&eacute;l&eacute;ment concern&eacute; (e.g. &eacute;paisseur de la ligne, 
couleur, couleur de remplissage).<br>On peut changer ces options pour un &eacute;l&eacute;ment existant apr&egrave;s
l&rsquo;avoir s&eacute;lectionn&eacute;. Ceci est &eacute;galement vrai pour l&rsquo;&eacute;l&eacute;ment suivant qui sera dessin&eacute; apr&egrave;s avoir s&eacute;lectionn&eacute; un outil de dessin.
</p>
<p class="hint">
On peut s&eacute;lection des &eacute;l&eacute;ments multiples pour les modifier en m&ecirc;me temps.
Pour ce faire, il suffit de maintenir la touche <kbd>Shift</kbd> enfonc&eacute;e tout en s&eacute;lectionnant les &eacute;l&eacute;ments.
</p>

<a name="editor-text"></a>
<h3>Ajouter du texte</h3>
<p>
L&rsquo;utilisation de l&rsquo;outil texte <kbd>T</kbd> est identique &agrave; celle des outils 
<a href="#editor-shapes">formes</a>. Il suffit de dessiner l&rsquo;&eacute;l&eacute;ment texte &agrave; la taille d&eacute;sir&eacute;e
puis de taper le texte souhait&eacute;.<br>
Double cliquer sur un texte existant pour l&rsquo;&eacute;diter.
</p>

<a name="editor-highlight"></a>
<h3>Accentuer des &eacute;l&eacute;ments</h3>
<p>
Apr&egrave;s avoir s&eacute;lectionn&eacute; l&rsquo;outil <kbd>H</kbd>, on peut d&eacute;finir la surface que l&rsquo;on veut 
accentuer exactement de la m&ecirc;me fa&ccedil;on que l&rsquo;on dessinerait une <a href="#editor-shapes">forme</a>.<br>
On peut choisir entre plusieurs options d&rsquo;accentuation en cliquant 
sur le bouton &agrave; l&rsquo;extr&ecirc;me gauche du sommet de la barre d&rsquo;outil :
</p>
<ul>
<li><em>Accentuer un texte</em>: applique une couleur brillante sur le texte, comme
le fait un accentuateur de texte de bureau.</li>
<li><em>Accentuer une surface</em>: Brouille <a href="#hint-blur">*</a> et assombrit tout ce qui se trouve &agrave; l&rsquo;ext&eacute;rieur de la surface s&eacute;lectionn&eacute;e.</li>
<li><em>&Eacute;chelle de gris</em>: Tout ce qui se trouve en dehors de la zone s&eacute;lectionn&eacute;e passe en &eacute;chelle de gris.</li>
<li><em>Grossir</em>: La zone s&eacute;lectionn&eacute;e sera grossie.</li>
</ul>

<a name="editor-obfuscate"></a>
<h3>Obscurcir des &eacute;l&eacute;ments</h3>
<p>
Obscurcir des parties de capture d&rsquo;&eacute;cran permet d&eacute;viter de porter &agrave; la vue de tous des informations
qui ne leur sont pas destin&eacute;es, par exemple : donn&eacute;es de compte bancaire, noms, pots de passe, visages sur des images, etc.<br>
On utilise l&rsquo;outil d&rsquo;obscurcicement <kbd>O</kbd> exactement comme celui d&rsquo;<a href="#editor-highlight">accentuation.</a> 
<br>
Les options d&rsquo;obscurcicement disponibles sont :
</p>
<ul>
<li><em>Pixeliser</em>: augment la taille des pixels dans la zone s&eacute;lectionn&eacute;e</li>
<li><em>Brouillage </em><a href="#hint-blur">*</a>: Brouille la zone s&eacute;lectionn&eacute;e</li>
</ul>
<a name="hint-blur"></a>
<p class="hint">
* Suivant les performances de l'ordinateur, l&rsquo;application d'un effet de brouillage pourrait ralentir 
l&rsquo;&eacute;diteur d&rsquo;image de Greenshot.<br>Si l&rsquo;on constate que l&rsquo;&eacute;diteur d&rsquo;image r&eacute;agit lentement apr&egrave;s 
l&rsquo;application d&rsquo;un brouillage, essayer de r&eacute;duire la valeur de la <em>qualit&eacute; de l&rsquo;aper&ccedil;u</em> dans la barre d&rsquo;outils ou  
diminuer la valeur du <em>rayon de brouillage</em>.<br>
Si les performance en brouillage restent telles qu&rsquo;il devient difficile de travailler, alors il est pr&eacute;f&eacute;rable d&rsquo;utiliser 
l&rsquo;effet de pixelisation.
</p>

<a name="editor-crop"></a>
<h3>Rogner la capture d&rsquo;&eacute;cran</h3>
<p>
Si l'on a juste besoin d&rsquo;une partie de l&rsquo;&eacute;cran captur&eacute;, on utilise l'outil de rognage <kbd>C</kbd>
pour le dimensionner &agrave; la surface d&eacute;sir&eacute;e.<br>
Après avoir s&eacute;lectionn&eacute; l&rsquo;outil de rognage, dessiner un rectangle sur la zone de la capture &agrave; conserver.
On peut modifier la taille de la surface s&eacute;lectionn&eacute;e comme tout autre &eacute;l&eacute;ment.<br>
Lorsque la s&eacute;lection est correcte, utiliser le bouton de confirmation dans la barre d&rsquo;outils ou
appuyer sur la touche <kbd>Entr&eacute;e</kbd>. le Rognage peut &ecirc;tre annul&eacute; en cliquant sur le bouton annuler ou en tapant  
sur la touche <kbd>ESC</kbd>.
</p>

<a name="editor-reuse-elements"></a>
<h3>R&eacute;utiliser des &eacute;l&eacute;ments dessin&eacute;s</h3>
<p>
Si l'on trouve que les m&ecirc;mes &eacute;l&eacute;ments sont utilis&eacute;s dans la plupart de vos captures d&rsquo;&eacute;cran
(par ex, un champ texte contenant le type de navigateur et sa version ou l&rsquo;obscurcicement du m&ecirc;me 
&eacute;l&eacute;ment<br>sur plusieurs captures d&rsquo;&eacute;cran) on peut r&eacute;&#45;utiliser ces &eacute;l&eacute;ments.<br>
S&eacute;lectionner <em>Sauvegarder des objets vers un fichier</em> &agrave; partir du menu <em>Objet</em> pour sauvegarder le jeu d&rsquo;&eacute;l&eacute;ments courants
pour les r&eacute;utiliser plus tard.<br><em>Charger des objets &agrave; partir du fichier</em> applique les m&ecirc;mes &eacute;l&eacute;ments 
&agrave; une autre capture d&rsquo;&eacute;cran.
</p>

<a name="editor-export"></a>
<h3>Exporter une capture d&rsquo;&eacute;cran</h3>
<p>
Apr&egrave;s l&rsquo;&eacute;dition d&rsquo;une capture d&rsquo;&eacute;cran, le r&eacute;sultat peut en &ecirc;tre export&eacute; pour diff&eacute;rentes applications,
d&eacute;pendant des besoins. Les options d&rsquo;exportation sont accessibles via le menu <em>Fichier</em>,<br>la barre d'outils au sommet ou &agrave; l&rsquo;aide des raccourcis suivants :
</p>
<ul>
<li><em>Enregistrer</em> <kbd>Ctrl</kbd> + <kbd>S</kbd>: Enregistre l&rsquo;image vers un fichier &#40;Si l&rsquo;image a d&eacute;j&agrave; &eacute;t&eacute; sauvegard&eacute;e, un autre dialogue affiche <em>Enregistrer sous...&#41;</em></li>
<li><em>Enregistrer sous...</em> <kbd>Ctrl</kbd> + <kbd>Maj &#40;Shift&#41;</kbd> + <kbd>S</kbd>: permet de choisir l&rsquo;endroit, le nom du fichier et le format d&rsquo;image du fichier &agrave; sauvegarder.</li>
<li><em>Copier l&rsquo;image vers le presse&#45;papier</em> <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>C</kbd>: place une copie de l&rsquo;image dans le presse&#45;papier, permettant ainsi de l&rsquo;utiliser dans un autre logiciel.</li>
<li><em>Imprimer...</em> <kbd>Ctrl</kbd> + <kbd>P</kbd>: envoie l&rsquo;image vers l&rsquo;imprimante.</li>
<li><em>E-Mail</em> <kbd>Ctrl</kbd> + <kbd>E</kbd>: ouvre un nouveau message dans votre client mail par d&eacute;faut, ajoutant l&rsquo;image en pi&egrave;ce jointe.</li>
</ul>
<p class="hint">
Après avoir enregistr&eacute; une image de l&rsquo;&eacute;diteur, faire un clic droit sur la barre d&rsquo;&eacute;tat en bas de 
le fen&ecirc;tre de l&rsquo;&eacute;diteur pour copier le chemin du fichier soit dans le presse-papier<br>soit pour ouvrir
le dossier correspondant dans le navigateur de Windows.
</p>


<a name="settings"></a>
<h2>Le dialogue de Param&egrave;trage</h2>

<a name="settings-general"></a>
<h3>R&eacute;glages g&eacute;n&eacute;raux</h3>
<ul>
<li><em>Langue</em>: La langue qu&rsquo;on pr&eacute;f&egrave;re utiliser.<br>
On peut t&eacute;l&eacute;charger des fichiers de langue additionnelle pour Greenshot <a target="_blank" href="http://getgreenshot.org/downloads/">Ici</a>. </li>
<li><em>Enregistrer des raccourcis</em>: si s&eacute;lectionn&eacute; Greenshot peut &ecirc;tre utilis&eacute; avec la touche <kbd>Impr &Eacute;cran</kbd>.</li>
<li><em>Lancer Greenshot au d&eacute;marrage</em>: D&eacute;marre le programme lorsque le syst&egrave;me d&rsquo;exploitation a &eacute;t&eacute; lanc&eacute;.</li>
<li><em>d&eacute;clencher un flash</em>: Retour visuel lorsqu&rsquo;on effectue une capture.</li>
<li><em>Jouer un son</em>: d&eacute;clenche un son lors d&rsquo;une capture.</li>
<li><em>Capturer le pointeur de souris</em>: Si s&eacute;lectionn&eacute;, le pointeur de souris sera captur&eacute;. Le pointeur est stock&eacute; comme &eacute;l&eacute;ment s&eacute;par&eacute; dans l&rsquo;&eacute;diteur, pour permettre de le d&eacute;placer ou de l&rsquo;enlever plus tard.</li>
<li><em>Utiliser le mode de capture de fen&ecirc;tre interactive</em>: Au lieu de capturer imm&eacute;diatement la fen&ecirc;tre active, le mode interactif
permet de s&eacute;lectionner la fen&ecirc;tre que l&rsquo;on souhaite capturer.<br>Il est &eacute;galement possible de capturer des fen&ecirc;tres s&oelig;ur, voir <a href="#capture-window">Capturer une fen&ecirc;tre</a>.</li>
</ul>

<a name="settings-output"></a>
<h3>Param&egrave;trage de sortie</h3>
<ul>
<li><em>Destination de la capture</em>: Permet de choisir la (les) destination(s) de la capture apr&egrave;s l&rsquo;avoir fait.</li>
<li><em>Choix du fichier de sortie</em>: D&eacute;fini le dossier et le nom du fichier &agrave; utiliser ou sugg&eacute;r&eacute; lors de l&rsquo;enregistrement &#40;utilisation du dialogue enregistrer&#45;sous&#41;.<br>actionner la touche <em>?</em> pour en conna&icirc;tre plus sur les param&egrave;tres qui peuvent &ecirc;tre utilis&eacute;s comme mod&egrave;le de fichier.</li>
<li><em>R&eacute;glages JPEG</em>: Choix de la qualit&eacute; &agrave; utiliser lors de l&rsquo;enregistrement d&rsquo;un fichier JPEG.</li>
</ul>

<a name="settings-printer"></a>
<h3>R&eacute;glage de l&rsquo;imprimante</h3>
<ul>
<li><em>Diminuer la taille de l&rsquo;impression pour correspondre au papier</em>: Si l&rsquo;image exc&egrave;de la taille du papier, l'image sera ajust&eacute;e à la taille du papier.</li>
<li><em>Augmenter la taille d'impression pour l&rsquo;ajuster &agrave; la taille du papier</em>: Si l&rsquo;image est plus petite que la taille du papier, elle sera ajust&eacute;e &agrave; la taille du papier sans l&rsquo;exc&eacute;der.</li>
<li><em>Tourner l&rsquo;image dans le sens de la page</em>: fera tourner une image au format paysage de 90° pour permettre son impression.</li>
</ul>


<a name="help"></a>
<h2>Vous souhaitez nous soutenir ?</h2>

<p>
Nous n&rsquo;avons pas besoin d&rsquo;aide pour d&eacute;velopper le logiciel actuellement. Cependant, il y a plusieurs choses qu'on peut 
faire pour supporter Greenshot et l&rsquo;&eacute;quipe de d&eacute;veloppement.<br>
Merci par avance :)
</p>

<a name="help-donate"></a>
<h3>Faire une donation</h3>
<p>
Nous travaillons &eacute;norm&eacute;ment sur Greenshot et passons &eacute;norm&eacute;ment de temps pour
fournir un excellent logiciel gratuit et libre.<br>Si vous &ecirc;tes plus efficace et s&rsquo;il vous permet de sauvegarder du temps et de l&rsquo;argent
ou si vous aimez simplement Greenshot et le principe des logiciels de source libre, vous pouvez encourager nos efforts par une donation.
<br>
Jetez un &oelig;il sur notre page de garde pour voir comment vous pouvez supporter l&rsquo;&eacute;quipe de developpement de Greenshot:<br>
<a target="_blank" href="http://getgreenshot.org/support/">http://getgreenshot.org/support/</a>
</p>

<a name="help-spread"></a>
<h3>Diffuser l&rsquo;information</h3>
<p>
Si vous aimez Greenshot, faites le savoir autour de vous, &agrave; vos amis et coll&egrave;gues.
Vos suiveurs &eacute;galement :)<br>
&Eacute;valuer Greenshot sur les portails du logiciel ou placez un lien dans votre blog ou votre site internet vers notre page de garde.
</p>

<a name="help-translate"></a>
<h3>Soumettre une traduction</h3>
<p>
Greenshot n'est pas disponible dans votre language pr&eacute;f&eacute;r&eacute;! Si vous vous sentez de traduire un logiciel,
vous &ecirc;tes plus que bienvenue.<br>
Si vous &ecirc;tes un utilisateur enregistr&eacute; de sourceforge.net, vous pouvez soumettre  votre traduction &agrave; notre 
<a target="_blank" href="https://sourceforge.net/tracker/?group_id=191585&atid=1368020">translations tracker</a>.<br>
Assurez&#45;vous qu&rsquo;aucune traduction n&rsquo;existe dans votre langue sur notre
<a target="_blank" href="http://getgreenshot.org/downloads/">downloads page</a>. V&eacute;rifiez &eacute;galement notre <a href="https://sourceforge.net/tracker/?group_id=191585&atid=1368020">translations tracker</a>,
  pour v&eacute;rifier qu&rsquo;il n&rsquo;y a aucune traduction en cours ou en discussion.<br>
Notez bien que nous ne fournirons cette traduction seulement si elle a &eacute;t&eacute; soumise via un compte d&rsquo;utilisateur sourceforge.net. 
Du fait que selon toute probabilit&eacute; nous serons incapables de comprendre<br>votre traduction ; il est logique que d&rsquo;autres
utilisateurs de sourceforge puissent vous contacter pour l'am&eacute;liorer ou d&eacute;finir une nouvelle version. 
</p>
