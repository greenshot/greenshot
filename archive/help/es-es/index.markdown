---
layout: page
status: publish
published: true
title: Ayuda de Greenshot
author:
  display_name: greenshot
  login: admin
  email: greenshot-developers@lists.sourceforge.net
  url: http://getgreenshot.org/
author_login: admin
author_email: greenshot-developers@lists.sourceforge.net
author_url: http://getgreenshot.org/
wordpress_id: 382
wordpress_url: http://getgreenshot.org/
date: !binary |-
  MjAxMi0wNC0wOCAxOTo1MTowOCArMDIwMA==
date_gmt: !binary |-
  MjAxMi0wNC0wOCAxNzo1MTowOCArMDIwMA==
categories: []
tags: []
comments: []
---
<p>Version 0.8<br />
	<small>Spanish translation of help content by Alejandro de G&aacute;rate<br />
	&lt;alex_degarate AT yahoo DOT com&gt; </small></p>
<h2>Contenido</h2>
<ol>
<li><a href="#screenshot">Realizando una captura de pantalla</a></li>
<ol>
<li><a href="#capture-region">Capturar regi&oacute;n</a></li>
<li><a href="#capture-last-region">Capturar &uacute;ltima regi&oacute;n</a></li>
<li><a href="#capture-window">Capturar ventana</a></li>
<li><a href="#capture-fullscreen">Capturar pantalla completa</a></li>
</ol>
<p><br>	</p>
<li><a href="#editor">Usando el editor de imagen</a></li>
<ol>
<li><a href="#editor-shapes">Dibujando formas</a></li>
<li><a href="#editor-text">Agregando texto</a></li>
<li><a href="#editor-highlight">Resaltando cosas</a></li>
<li><a href="#editor-obfuscate">Oscureciendo cosas</a></li>
<li><a href="#editor-crop">Recortando la captura de pantalla</a></li>
<li><a href="#editor-reuse-elements">Reusando elementos dibujados</a></li>
<li><a href="#editor-export">Exportando la captura de pantalla</a></li>
</ol>
<p><br></p>
<li><a href="#settings">Di&aacute;logo de configuraci&oacute;n</a></li>
<ol>
<li><a href="#settings-general">Configuraci&oacute;n general</a></li>
<li><a href="#settings-output">Configuraci&oacute;n de salida</a></li>
<li><a href="#settings-printer">Configuraci&oacute;n de la impresora</a></li>
</ol>
<p><br></p>
<li><a href="#help">&iquest; Desea ayudar ?</a></li>
<ol>
<li><a href="#help-donate">Considere una donaci&oacute;n</a></li>
<li><a href="#help-spread">Corra la voz</a></li>
<li><a href="#help-translate">Env&iacute;e una traducci&oacute;n</a></li>
</ol>
</ol>
<p>	<a name="screenshot"></a></p>
<h2>Realizando una captura de pantalla</h2>
<p>
	Ud puede realizar una captura de pantalla, bien usando la tecla <kbd>Print</kbd><br />
	de su teclado o abriendo una men&uacute; de opciones al pulsar el bot&oacute;n<br />
        derecho del rat&oacute;n sobre el icono de Greenshot en la barra de tareas del sistema.<br><br />
	Hay varias opciones para realizar una captura de pantalla:
	</p>
<p>	<a name="capture-region"></a></p>
<h3>Capturar regi&oacute;n <kbd>Print</kbd></h3>
<p>
	El modo <b>capturar regi&oacute;n</b> le permite seleccionar que parte de su<br />
        pantalla ser&aacute; capturada. <br><br />
	Despu&eacute;s de comenzar el modo regi&oacute;n, usted ver&aacute; una l&iacute;nea<br />
        vertical y otra horizontal que se cruzan en un punto controlado por el rat&oacute;n.<br />
        Haga clic y mantenga apretado el bot&oacute;n izquierdo del rat&oacute;n donde usted<br />
        desee que est&eacute; una de las esquinas del rect&aacute;ngulo a capturar.<br />
        Manteniendo apretado el bot&oacute;n del rat&oacute;n, arrastre el rat&oacute;n para<br />
        definir el rect&aacute;ngulo a ser capturado. Cuando el rect&aacute;ngulo verde<br />
        cubra el &aacute;rea que usted desea que sea capturada, suelte el bot&oacute;n<br />
        del rat&oacute;n.
	</p>
<p class="hint">
	  Ud. puede usar la tecla <kbd>Space</kbd> para cambiar entre el modo<br />
	  regi&oacute;n y el modo <a href="#capture-window">ventana</a>.
	</p>
<p class="hint">
        Si usted desea capturar un &aacute;rea exacta, podr&iacute;a ser m&aacute;s<br />
        f&aacute;cil seleccionar el &aacute;rea inical de captura ligeramente mayor y<br />
        <a href="#editor-crop">recortar</a> la captura de pantalla posteriormente usando<br />
	el editor de im&aacute;genes de Greenshot.
	</p>
<p>	<a name="capture-last-region"></a></p>
<h3>Capturar &uacute;ltima regi&oacute;n <kbd>Shift</kbd> + <kbd>Print</kbd></h3>
<p>
	Si usted reci&eacute;n realiz&oacute; una captura de <a href="#capture-region">regi&oacute;n</a><br />
        o de <a href="#capture-window">ventana</a>, Ud. puede capturar la misma<br />
        regi&oacute;n de nuevo usando esta opci&oacute;n.
	</p>
<p>	<a name="capture-window"></a></p>
<h3>Capturar ventana <kbd>Alt</kbd> + <kbd>Print</kbd></h3>
<p>
	Realiza una captura de pantalla de la ventana actualmente activa.
	</p>
<p class="hint">
	El <a href="#settings">di&aacute;logo de configuraci&oacute;n</a> ofrece una<br />
	opci&oacute;n, no para capturar la ventana activa de inmediato, sino permitiendo<br />
	a usted seleccionar una interactivamente.<br />
	Si esta opci&oacute;n es establecida, usted puede seleccionar una ventana<br />
        haciendo clic en ella (como en el modo <a href="#capture-region">capturar regi&oacute;n</a>,<br />
        Greenshot resaltar&aacute; el &aacute;rea que ser&aacute; capturada).<br><br />
        Si usted desea que una ventana secundaria sea capturada (por ej. ventana del navegador<br />
        (sin barra de herramientas, etc.) o un simple cuadro de una p&aacute;gina web<br />
        usando "framesets") apunte con el cursor del rat&oacute;n a la ventana y pulse<br />
        la tecla <kbd>PgDown</kbd>. Despu&eacute;s de hacerlo, usted puede seleccionar<br />
        elementos secundarios de la ventana para ser capturados. <br>
	</p>
<p>	<a name="capture-fullscreen"></a></p>
<h3>Capturar pantalla completa <kbd>Control</kbd> + <kbd>Print</kbd></h3>
<p>
	 Realiza una captura de la pantalla completa.
        </p>
<p>	<a name="editor"></a></p>
<h2>Usando el editor de imagen</h2>
<p>
	Greenshot viene con un editor de im&aacute;genes f&aacute;cil-de-usar,<br />
        proveyendo un pr&aacute;ctico y c&oacute;modo conjunto de caracter&iacute;sticas<br />
        para agregar anotaciones o formas a la captura de pantalla. Esta incluso permite<br />
        resaltar u oscurecer partes de su captura de pantalla.
	</p>
<p class="hint">
	El editor de im&aacute;genes de Greenshot puede ser usado no solamente para<br />
        captura de pantallas. Usted tambi&eacute;n puede abrir im&aacute;genes para<br />
        editar desde un archivo o desde el portapapeles. Simplemente oprima el<br />
        bot&oacute;n derecho del rat&oacute;n sobre el icono de Greenshot en la barra<br />
        de tareas del sistema y seleccione <em><b>Abrir imagen desde archivo</b></em> o<br />
        <em><b>Abrir imagen desde el portapapeles</b></em>, respectivamente.
	</p>
<p class="hint">
	Por defecto, el editor de im&aacute;genes ser&aacute; abierto cada vez que una<br />
        imagen de pantalla sea capturada. Si usted no desea usar el editor de im&aacute;genes,<br />
        puede deshabilitar este comportamiento en el<br />
        <a href="#settings">di&aacute;logo de configuraci&oacute;n</a>.
	</p>
<p>	<a name="editor-shapes"></a></p>
<h3>Dibujando formas</h3>
<p>
	Seleccione una de las formas desde la barra de herramientas sobre el lado izquierdo<br />
        del editor de im&aacute;genes o desde el men&uacute; <em><b>Objeto</b></em>. Tambi&eacute;n<br />
        hay una tecla asignada a cada herramienta para su conveniencia.<br><br />
	Formas disponibles son: rect&aacute;ngulo <kbd>R</kbd>, elipse <kbd>E</kbd>,<br />
        l&iacute;nea <kbd>L</kbd> y flecha <kbd>A</kbd>.<br><br />
	Haga clic, y mantenga apretado el bot&oacute;n del rat&oacute;n y arrastre<br />
        para definir la posici&oacute;n y tama&ntilde;o de la forma.<br />
	Suelte el bot&oacute;n del rat&oacute;n cuando haya terminado.
	</p>
<p>
	Usted puede mover o redimensionar formas existentes despu&eacute;s de elegir la<br />
        <em><b>Herramienta de Selecci&oacute;n</b></em> <kbd>ESC</kbd> desde la barra de herramientas.<br<br />
        Para cada tipo de elemento hay un conjunto espec&iacute;fico de opciones disponibles<br />
        para cambiar la apariencia del elemento (por ej. grosor de la l&iacute;nea, color<br />
        de la l&iacute;nea, color de relleno). Usted puede cambiar las opciones para un<br />
        elemento existente despu&eacute;s de seleccionarlo, pero tambi&eacute;n para el<br />
        pr&oacute;ximo elemento a ser dibujado despu&eacute;s de elegir una herramienta<br />
        de dibujo.
	</p>
<p class="hint">
	Usted puede seleccionar m&uacute;ltiples elementos a la vez para ser editados.<br />
        Con el fin de seleccionar varios elementos, mantenga apretada la tecla<br />
        <kbd>Shift</kbd> mientras hace clic en los elementos.
	</p>
<p>	<a name="editor-text"></a></p>
<h3>Agregando texto</h3>
<p>
	El uso de la herramienta de texto <kbd>T</kbd> es similar al uso de la herramienta<br />
	<a href="#editor-shapes">forma</a>. <br><br />
        S&oacute;lo dibuje el elemento de texto del tama&ntilde;o deseado, y a<br />
        continuaci&oacute;n  escriba el texto en &eacute;l.<br><br />
	Haga doble clic en un elemento de texto existente para editar el texto.
	</p>
<p>	<a name="editor-highlight"></a></p>
<h3>Resaltando cosas</h3>
<p>
	Despu&eacute;s de elegir la herramienta Resaltar <kbd>H</kbd>, usted puede<br />
        definir el &aacute;rea a ser resaltada de la misma manera que dibuja una<br />
	<a href="#editor-shapes">forma</a>.<br><br />
	Hay varias opciones para la herramienta resaltar, que usted puede elegir al hacer clic<br />
	en el bot&oacute;n m&aacute;s a la izquierda de la barra de herramientas superior:
	</p>
<ul>
<li><em><b>Resaltar texto</b></em>: Resalta un &aacute;rea al aplicar un color brillante a ella, tal como<br />
		   un l&aacute;piz resaltador de oficina</li>
<li><em><b>Resaltar &aacute;rea</b></em>: difumina<a href="#hint-blur">*</a> y oscurece todo fuera del &aacute;rea elegida</li>
<li><em><b>Escala de Grises</b></em>: todo fuera del &aacute;rea elegida ser&aacute; convertido a escala de grises</li>
<li><em><b>Magnificar</b></em>: el &aacute;rea elegida ser&aacute; mostrada en forma agrandada</li>
</ul>
<p>	<a name="editor-obfuscate"></a></p>
<h3>Oscureciendo cosas</h3>
<p>
	Oscurecer partes de una captura de pantalla es una buena idea, si esta contiene<br />
        datos que no se desea que otra gente los vea, por ej. datos de cuenta bancaria,<br />
        nombres, contrase&ntilde;as o caras en im&aacute;genes.<br><br />
	Use la herramientas de oscurecer <kbd>O</kbd> exactamente como en la herramienta<br />
        <a href="#editor-highlight">resaltar</a>.<br><br />
	Opciones disponibles para oscurecimiento son:
	</p>
<ul>
<li><em><b>Pixelar</b></em>: incrementa el tama&ntilde;o del pixel para el &aacute;rea elegida</li>
<li><em><b>Difuminar</b> (Blur)</em> <a href="#hint-blur">*</a>: difumina el &aacute;rea elegida</li>
</ul>
<p>	<a name="hint-blur"></a></p>
<p class="hint">
	* Dependiendo del desempe&ntilde;o de su computadora, aplicar un efecto <i>difuminar</i><br />
        podr&iacute;a enlentecer al editor de im&aacute;genes de Greenshot.<br />
	Si usted siente que el editor de im&aacute;genes reacciona lentamente tan pronto<br />
        como el efecto de difuminar es aplicado, intente desminuir el valor para<br />
        <em><b>Calidad de vista previa</b></em> o desminuir el valor de <em><b>Radio de difuminar</b></em><br />
        en la barra de herramientas. <br><br />
	Si el desempe&ntilde;o de difuminar todav&iacute;a es muy malo para trabajar con<br />
        &eacute;l, usted podr&iacute;a preferir usar el efecto <i>pixelar</i> en su lugar.
	</p>
<p>	<a name="editor-crop"></a></p>
<h3>Recortando la captura de pantalla</h3>
<p>
	Si usted solamente necesita una parte de la captura de pantalla, use la herramienta<br />
        Recortar pulsando la tecla <kbd>C</kbd> para recortar el &aacute;rea deseada.<br><br />
	Despu&eacute;s de selecionar la herramienta de recortar, dibuje un rect&aacute;ngulo para<br />
        el &aacute;rea de la captura de pantalla que usted desea conservar. Usted puede cambiar<br />
        el tama&ntilde;o del &aacute;rea seleccionada como en cualquier otro elemento.<br><br />
	Cuando usted est&eacute; conforme con su selecci&oacute;n, use el bot&oacute;n de<br />
        confirmaci&oacute;n en la barra de herramientas o pulse la tecla <kbd>Enter</kbd>.<br />
        Usted puede cancelar el recorte al hacer clic en el bot&oacute;n cancel o pulsando<br />
	<kbd>ESC</kbd>.
	</p>
<p>	<a name="editor-reuse-elements"></a></p>
<h3>Reusando elementos dibujados</h3>
<p>
	Si usted se encuentra a si mismo usando id&eacute;nticos o similares elementos<br />
        en la mayor&iacute;a de sus capturas de pantalla (por ej. un campo de texto<br />
        conteniendo tipo de navegador y versi&oacute;n, u oscureciendo el mismo elemento<br />
        en varias capturas de pantalla) usted puede reusar elementos.<br><br />
 	Seleccione <em><b>Guardar objetos a archivo</b></em> desde el men&uacute; <em><b>Objeto</b></em><br />
        para guardar el conjunto actual de elementos para reusarlo luego. <br><br />
	<em><b>Cargar objetos desde archivo</b></em> aplica los mismos elementos a otra<br />
	captura de pantalla. <br><br>
	</p>
<p>	<a name="editor-export"></a></p>
<h3>Exportando la captura de pantalla</h3>
<p>
	Despu&eacute;s de editar la captura de pantalla, usted puede exportar el resultado<br />
        para diferentes prop&oacute;sitos, dependiendo de sus necesidades. Usted puede<br />
        acceder todas las opciones de exportar a trav&eacute;s del men&uacute;<br />
        <em><b>Archivo</b></em> en la barra de herramientas superior o via teclas de<br />
        acceso r&aacute;pido:
	</p>
<ul>
<li><em>Guardar</em> <kbd>Control</kbd> + <kbd>S</kbd>: guarda la imagen a un<br />
             archivo (si la imagen no ha sido guardada), de lo contrario muestra el di&aacute;logo <em>Guardar como...</em> </li>
<li><em>Guardar como...</em> <kbd>Control</kbd> + <kbd>Shift</kbd> + <kbd>S</kbd>:<br />
               le permite a usted elegir ubicaci&oacute;n, nombre de archivo y formato<br />
               de imagen para el archivo a guardar.</li>
<li><em>Copiar imagen al portapapeles</em> <kbd>Control</kbd> + <kbd>Shift</kbd> + <kbd>C</kbd>:<br />
               pone una copia de la imagen dentro del portapapeles, permitiendo pegar la<br />
               imagen dentro de otros programas</li>
<li><em>Imprimir...</em> <kbd>Control</kbd> + <kbd>P</kbd>: env&iacute;a la imagen a la impresora</li>
<li><em>Enviar correo</em> <kbd>Control</kbd> + <kbd>E</kbd>: abre un nuevo<br />
               mensaje en su cliente de e-mail por defecto, agregando la imagen como<br />
               un arhivo adjunto</li>
</ul>
<p class="hint">
	Despu&eacute;s de guardar una imagen desde el editor, pulse con el bot&oacute;n<br />
        derecho del rat&oacute;n en la barra de estado que se ecuentra en la parte de<br />
        abajo de la ventana del editor, ya sea para copiar la ruta del archivo dentro<br />
        del portapapeles o abrir el directorio que la contiene en el Explorador de Windows.
	</p>
<p>	<a name="settings"></a></p>
<h2>Di&aacute;logo de configuraci&oacute;n</h2>
<p>	<a name="settings-general"></a></p>
<h3>Configuraci&oacute;n general</h3>
<ul>
<li><em><b>Idioma</b></em>: El idioma que usted prefiera usar de los disponibles en<br />
            el men&uacute; desplegable.<br />
	    Usted puede descargar archivos adicionales de idioma para Greenshot <a href="#">aqui</a>. </li>
<li><em><b>Registrar teclas de acceso r&aacute;pido</b></em>: Si esta<br />
             opci&oacute;n es establecida, Greenshot puede ser usado con la tecla <kbd>Print</kbd>.</li>
<li><em><b>Lanzar Greenshot al arrancar el sistema</b></em>: Iniciar el programa cuando el sistema ha sido arrancado.</li>
<li><em><b>Mostrar destello</b></em>: Retorno visual cuando realice una captura</li>
<li><em><b>Reproducir sonido</b></em>: Retorno audible cuando realice una captura</li>
<li><em><b>Capturar puntero del rat&oacute;n</b></em>: Si esta opci&oacute;n<br />
             es establecida, el puntero del rat&oacute;n ser&aacute; capturado.<br />
             El puntero es manejado como un elemento separado en el editor, asi que<br />
             usted puede moverlo o removerlo luego.</li>
<li><em><b>Uso modo interactivo de captura de ventana</b></em>: En lugar de<br />
             capturar la ventana activa de inmediato, el modo interactivo le permite a<br />
             usted seleccionar la ventana a capturar. Tambie&eacute;n es posible capturar<br />
             ventanas secundarias, vea <a href="#capture-window">captura de ventanas</a>.</li>
</ul>
<p>	<a name="settings-output"></a></p>
<h3>Configuraci&oacute;n de salida</h3>
<ul>
<li><em><b>Destino de la captura de pantalla</b></em>:<br />
                Le permite a usted elegir el destino de su captura de pantalla<br />
                inmediatamente despu&eacute;s de capturarla.</li>
<li><em><b>Configuraci&oacute;n preferida al guardar archivo</b></em>:<br />
                Directorio y nombre de archivo a ser usado cuando se graba directamente<br />
                o para ser sugerido cuando se guarda (usando el dialogo "Guardar como").<br><br />
                Haga clic en el bot&oacute;n <b>?</b> para aprender m&aacute;s acerca<br />
                marcadores de posici&oacute;n que pueden ser usados como patrones en<br />
                nombre de archivo.</li>
<li><em><b>Configuraci&oacute;n JPEG</b></em>:<br />
                Calidad a ser usada cuando se guarda la imagen como archivo JPEG.</li>
</ul>
<p>	<a name="settings-printer"></a></p>
<h3>Configuraci&oacute;n de la impresora</h3>
<ul>
<li><em><b>Reducir impresi&oacute;n hasta ajustar al tama&ntilde;o del papel</b></em>:<br />
             Si la imagen pudiera exceder el tama&ntilde;o del papel, esta ser&aacute;<br />
             reducida hasta ajustar a la p&aacute;gina.</li>
<li><em><b>Agrandar impresi&oacute;n hasta ajustar al tama&ntilde;o del papel</b></em>:<br />
             Si la imagen es menor tama&ntilde;o del papel, esta ser&aacute; escalada<br />
             para ser impresa tan grande como sea posible sin exceder el tama&ntilde;o<br />
             del papel.</li>
<li><em><b>Rotar impresi&oacute;n segun orientaci&oacute;n de la p&aacute;gina</b></em>:<br />
             Rota la imagen 90 grados a un formato apaisado para su impresi&oacute;n.</li>
</ul>
<p>	<a name="help"></a></p>
<h2>&iquest; Desea ayudar ?</h2>
<p>
	Actualmente, nosotros no necesitamos ayuda para desarrollo. Sin embargo, hay<br />
        varias cosas que usted puede hacer para ayudar a Greenshot y al equipo de<br />
        desarrollo.<br><br />
		Gracias por adelantado :)
	</p>
<p>	<a name="help-donate"></a></p>
<h3>Considere una donaci&oacute;n</h3>
<p>
	Nosotros estamos poniendo un mont&oacute;n de trabajo en Greenshot y pasando<br />
        bastante tiempo para proveer una buena pieza de software gratis y con<br />
        c&oacute;digo fuente. Si usted siente que este programa lo hace m&aacute;s<br />
        productivo, le ahorra a usted (o a su compa&ntilde;ia) un mont&oacute;n de<br />
        tiempo y dinero, o si usted simplemente gusta de Greenshot y la idea del<br />
        software de c&oacute;digo abierto: por favor considere honrar nuestro esfuerzo<br />
        con una donaci&oacute;n.<br><br />
	Por favor visite nuestra p&aacute;gina web para ver como usted puede apoyar al<br />
        equipo de desarrollo de Greenshot:<br><br />
	<a href="/support/">http://getgreenshot.org/support/</a>
	</p>
<p>	<a name="help-spread"></a></p>
<h3>Corra la voz</h3>
<p>
	Si usted gusta de Greenshot, permita que la gente sepa: cu&eacute;ntele a sus<br />
        amigos y colegas acerca de Greenshot.<br />
	Sus seguidores, tambi&eacute;n :)<br><br />
	Califique a Greenshot en los portales de software o ponga un enlace a nuestra<br />
        p&aacute;gina de inicio desde su blog o sitio web.
	</p>
<p>	<a name="help-translate"></a></p>
<h3>Env&iacute;e una traducci&oacute;n</h3>
<p>
	&iquest; Greenshot no esta disponible en su lenguaje preferido ?<br />
        Si usted se siente capaz para traducir una pieza de software, usted es m&aacute;s<br />
        que bienvenido.<br />
	Si usted es un usuario registrado en sourceforge.net, usted puede enviar<br />
        traducciones a nuestro<br />
<a href="https://sourceforge.net/tracker/?group_id=191585&atid=1368020">seguimiento de traducciones</a>.<br><br />
	Por favor aseg&uacute;rese que no exista traducci&oacute;n para su idioma en nuestra<br />
	<a href="#">p&aacute;gina de descargas</a>. Tambi&eacute;n visite nuestra<br />
 <a href="https://sourceforge.net/tracker/?group_id=191585&atid=1368020">seguimiento de traducciones</a>,<br />
	alli podr&iacute;a haber una traducci&oacute;n en progreso, o al menos en<br />
        discusi&oacute;n.<br><br />
	Por favor note que nosotros solamente proveeremos una traducci&oacute;n en<br />
        nuestra p&aacute;gina de descargas si esta ha sido presentada a trav&eacute;s de<br />
        su cuenta de usuario en sourceforge.net.<br />
        Puesto que lo m&aacute;s probable es que nosotros no seamos capaces de entender<br />
        su traducci&oacute;n, es bueno que otros usuarios de sourceforge puedan contactarlo<br />
        acerca de mejoras y ampliaciones en caso de una nueva versi&oacute;n de Greenshot.
	</p>
