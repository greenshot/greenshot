---
layout: page
status: publish
published: true
title: Ajuda do Greenshot
author:
  display_name: greenshot
  login: admin
  email: greenshot-developers@lists.sourceforge.net
  url: http://getgreenshot.org/
author_login: admin
author_email: greenshot-developers@lists.sourceforge.net
author_url: http://getgreenshot.org/
wordpress_id: 1136
wordpress_url: http://getgreenshot.org/?page_id=1136
date: !binary |-
  MjAxNS0wMi0wNCAyMjo0MzoyMiArMDEwMA==
date_gmt: !binary |-
  MjAxNS0wMi0wNCAyMDo0MzoyMiArMDEwMA==
categories: []
tags: []
comments: []
---
<h1>Ajuda do Greenshot</h1>
<p>	<!--<br />
	Note to translator: uncomment entry below to have your effort honored<br />
	--><br />
	Versão 1.0<br />
	<small>Tradução portuguesa do conteúdo da Ajuda por Luis Neves (luis.a.neves@sapo.pt)</small></p>
<h2>Conteúdo</h2>
<ol>
<li><a href="#screenshot">Criar uma captura de ecrã</a></li>
<ol>
<li><a href="#capture-region">Capturar uma região</a></li>
<li><a href="#capture-last-region">Capturar a última região</a></li>
<li><a href="#capture-window">Capturar janela</a></li>
<li><a href="#capture-fullscreen">Capturar o ecrã completo</a></li>
<li><a href="#capture-ie">Capturar Internet Explorer</a></li>
</ol>
<li><a href="#editor">Usar o editor de imagem</a></li>
<ol>
<li><a href="#editor-shapes">Desenhar formas</a></li>
<li><a href="#editor-text">Adicionar texto</a></li>
<li><a href="#editor-highlight">Realçar coisas</a></li>
<li><a href="#editor-obfuscate">Ofuscar coisas</a></li>
<li><a href="#editor-effects">Adicionar efeitos</a></li>
<li><a href="#editor-crop">Recortar a captura de ecrã</a></li>
<li><a href="#editor-adding-graphics">Adicionar gráficos a uma captura</a></li>
<li><a href="#editor-reuse-elements">Reutilizar elementos desenhados</a></li>
<li><a href="#editor-export">Exportar a captura</a></li>
</ol>
<li><a href="#settings">Definições</a></li>
<ol>
<li><a href="#settings-general">Definições gerais</a></li>
<li><a href="#settings-capture">Definições de captura</a></li>
<li><a href="#settings-output">Definições de Saída</a></li>
<li><a href="#settings-destination">Definições de destino</a></li>
<li><a href="#settings-printer">Definições de Impressão</a></li>
<li><a href="#settings-expert">Definições avançadas</a></li>
</ol>
<li><a href="#help">Quer ajudar?</a></li>
<ol>
<li><a href="#help-donate">Faça um donativo</a></li>
<li><a href="#help-spread">Passe a palavra</a></li>
<li><a href="#help-translate">Envie uma tradução</a></li>
</ol>
</ol>
<p>	<a name="screenshot"></a></p>
<h2>Criar uma captura de ecrã</h2>
<p>
		Pode criar uma captura de ecrã ou usando a tecla <kbd>Print</kbd> do seu teclado<br />
		ou clicando com o botão direito do rato no ícone Greenshot da bandeja do sistema.<br><br />
		Existem diversas opções para criar uma captura de ecrã:
	</p>
<p>	<a name="capture-region"></a></p>
<h3>Capturar região <kbd>Print</kbd></h3>
<p>
		O modo de capturar uma região permite-lhe seleccionar uma parte do ecrã a ser capturada.<br><br />
		Após iniciar o modo região, irá visualizar uma mira a apontar a posição do rato<br />
		no ecrã. Clique e mantenha o botão premido no local onde pretende que fique um dos cantos<br />
		da sua captura. Mantendo o botão do rato premido, arraste o rato para definir o<br />
		rectângulo a ser capturado. Quando o rectângulo verde cobrir a área que pretende<br />
		capturar, liberte o botão do rato.
	</p>
<p class="hint">
		Pode usar a tecla <kbd>Space</kbd> para alternar entre o modo região e o modo<br />
		<a href="#capture-window">janela</a>.
	</p>
<p class="hint">
		Se pretende capturar uma área exacta, será mais fácil seleccionar a área inicial da<br />
		captura um pouco maior e depois <a href="#editor-crop">recortar</a> a captura de ecrã<br />
		usando o editor de imagem do Greenshot.
	</p>
<p>	<a name="capture-last-region"></a></p>
<h3>Capturar a última região <kbd>Shift</kbd> + <kbd>Print</kbd></h3>
<p>
		Se acabou de capturar uma <a href="#capture-region">região</a> ou uma <a href="#capture-window">janela</a>,<br />
		pode capturar novamente a mesma região usando esta opção.
	</p>
<p>	<a name="capture-window"></a></p>
<h3>Capturar uma janela <kbd>Alt</kbd> + <kbd>Print</kbd></h3>
<p>
		Cria uma captura da janela que está actualmente activa.
	</p>
<p class="hint">
		Para sistemas com o Windows 7 ou Vista, use a definição do modo Captura de Janela para controlar<br />
		como o Greenshot manipula as bordas da janela semi transparente (Aero), p.e. substituindo a<br />
		transparência com uma cor personalizada.
	</p>
<p class="hint">
		O <a href="#settings">diálogo de definições</a> oferece uma opção de não capturar<br />
		a janela activa de imediato, mas permitindo-lhe seleccionar uma interactivamente.<br />
		Se esta opção for seleccionada, pode seleccionar uma janela clicando nela (Tal como no<br />
		<a href="#capture-region">modo região</a>, o Greenshot irá realçar a área que<br />
		irá ser capturada).<br>Se pretender capturar uma janela secundária (p.e. uma janela do<br />
		navegador (sem barras de ferramentas, etc.) ou um simples quadro de uma página web que use 'framesets')<br />
		aponte o cursor do rato para a janela e prima a tecla <kbd>PgDown</kbd>. Depois de<br />
		fazer isso, pode seleccionar elementos secundários da janela para serem capturados.
	</p>
<p class="hint">
		A captura dos menus de contexto é diferente: ao usar o atalho "Capturar janela"<br />
		fará desaparecer o menu de contexto, e óbviamente acontecerá o mesmo se usar<br />
		o menu de contexto do Greenshot para criar a captura. Se pretende capturar<br />
		um menu de contexto que fez surgir clicando com o botão direito do rato em qualquer coisa,<br />
		simplesmente active o modo região <kbd>Print</kbd>, e depois prima a tecla <kbd>Space</kbd>.
	</p>
<p>	<a name="capture-fullscreen"></a></p>
<h3>Capturar ecrã completo <kbd>Control</kbd> + <kbd>Print</kbd></h3>
<p>
		Cria uma captura da totalidade do ecrã.
	</p>
<p>	<a name="capture-ie"></a></p>
<h3>Capturar Internet Explorer <kbd>Control</kbd> + <kbd>Shift</kbd> + <kbd>Print</kbd></h3>
<p>
		Cria confortavelmente uma captura da página web actualmente aberta no Internet Explorer.<br />
		Use o menu de contexto do Greenshot para seleccionar o separador do Internet Explorer a capturar, ou prima<br />
		<kbd>Crtl</kbd> + <kbd>Shift</kbd> + <kbd>Print</kbd> para capturar o separador activo.
	</p>
<p>	<a name="editor"></a></p>
<h2>Usar o editor de imagem</h2>
<p>
		O Greenshot possui um editor de imagem fácil de utilizar, fornecendo uma série de funcionalidades<br />
		para adicionar anotações e formas a uma captura. Ele também permite realçar ou<br />
		ofuscar partes da sua captura.
	</p>
<p class="hint">
		O editor de imagem do Greenshot pode ser usado não apenas em capturas. Também pode<br />
		abrir imagens de um ficheiro ou da área de transferência para editar. Basta clicar com o botão<br />
		direito do rato no ìcone do Greenshot na bandeja do sistema e seleccionar <em>Abrir imagem de ficheiro</em><br />
		ou <em>Abrir imagem da área de transferência</em>, respectivamente.
	</p>
<p class="hint">
		Por defeito, o editor de imagem é aberto sempre que é feita uma captura de ecrã.<br />
		Se não pretender usar o editor de imagem, pode desactivar esta<br />
		funcionalidade no <a href="#settings">diálogo definições</a>.
	</p>
<p>	<a name="editor-shapes"></a></p>
<h3>Desenhar formas</h3>
<p>
		Seleccione uma das ferramentas de desenho de formas da barra de ferramentas situada do lado<br />
		esquerdo do editor de imagem ou do menu <em>Objecto</em>. Para sua conveniência, também existe<br />
		uma tecla associada a cada ferramenta.<br><br />
		As formas disponíveis são: rectângulo <kbd>R</kbd>, elipse <kbd>E</kbd>, linha <kbd>L</kbd>,<br />
		seta <kbd>A</kbd> e de linha livre <kbd>F</kbd>.<br><br />
		Clique, mantenha premido o botão do rato e arraste para definir a posição e tamanho da forma.<br />
		Liberte o botão do rato quando tiver terminado.
	</p>
<p>
		Pode mover ou redimensionar as formas existentes com a ferramenta de selecção<br />
		<kbd>ESC</kbd> da barra de ferramentas.<br>Para cada tipo de elemento existe um conjunto<br />
		próprio de opções disponíveis para alterar o aspecto do elemento.(p.e. espessura da linha,<br />
		cor da linha, cor de preenchimento). Pode alterar as opções de um elemento existente depois<br />
		de seleccioná-lo, mas também para o próximo elemento a ser desenhado após seleccionar uma ferramenta de desenho.
	</p>
<p class="hint">
		Pode seleccionar vários elementos para editar ao mesmo tempo. Para seleccionar vários<br />
		elementos, mantenha premida a tecla <kbd>Shift</kbd> enquanto clica nos elementos.
	</p>
<p class="hint">
		Se pretende desenhar formas com lados iguais (p.e. forçar um rectângulo a ser quadrado) mantenha<br />
		premida a tecla <kbd>Shift</kbd> ao desenhar. Ao desenhar linhas ou setas, manter premida a tecla <kbd>Shift</kbd><br />
		resulta no ângulo da linha ser rodado em passos de 15°.<br><br />
		Pode também usar o <kbd>Shift</kbd> se pretender redimensionar um objecto existente mantendo as suas proporções.
	</p>
<p class="hint">
		Ao desenhar ou redimensionar, pode manter premida a tecla <kbd>Ctrl</kbd> para ter o objecto ancorado<br />
		no seu centro geométrico. I.e. o objecto também é redimensionado na direcção oposta. (Isto<br />
		é muito útil se pretende desenhar uma elipse à volta de qualquer coisa na sua captura.)
	</p>
<p>	<a name="editor-text"></a></p>
<h3>Adicionar texto</h3>
<p>
		A utilização da ferramenta texto <kbd>T</kbd> é semelhante à das ferramentas de<br />
		<a href="#editor-shapes">forma</a>. Desenhe simplesmente o elemento de texto do<br />
		tamanho desejado, depois digite o texto no seu interior.<br><br />
		Faça duplo clique num elemento de texto existente para editar o seu conteúdo.<br><br />
		Prima <kbd>Return</kbd> ou <kbd>Enter</kbd> quando tiver concluído a edição.
	</p>
<p class="hint">
		Se necessita inserir quebras de linha dentro de uma caixa de texto, prima <kbd>Shift</kbd> + <kbd>Return</kbd> ou<br />
		<kbd>Shift</kbd> + <kbd>Enter</kbd>.
	</p>
<p>	<a name="editor-highlight"></a></p>
<h3>Realçar coisas</h3>
<p>
		Após seleccionar a ferramenta de realçar <kbd>H</kbd>, pode definir a área para ser<br />
		realçada do mesmo modo como desenharia uma <a href="#editor-shapes">forma</a>.<br><br />
		Existem diversas opções para realçar, entre as quais pode escolher clicando no<br />
		botão mais à esquerda da barra de ferramentas na parte superior:
	</p>
<ul>
<li><em>Realçar texto</em>: realça uma área aplicando-lhe uma cor brilhante, como<br />
			os marcadores de texto usados no escritório</li>
<li><em>Realçar área</em>: desfoca<a href="#hint-blur">*</a> e escurece tudo fora da área seleccionada</li>
<li><em>Escala Cinza</em>: tudo o que está fora da área seleccionada ficará na escala cinza</li>
<li><em>Ampliar</em>: a área seleccionada será exibida com ampliação</li>
</ul>
<p>	<a name="editor-obfuscate"></a></p>
<h3>Ofuscar coisas</h3>
<p>
		Ofuscar partes de uma captura é uma boa ideia se contiver dados que não se pretende que sejam<br />
		vistos por outras pessoas, p.e. dados de contas bancárias, nomes, palavras-passe ou caras na imagem.<br><br />
		Use a ferramenta ofuscar <kbd>O</kbd> exactamente do mesmo modo que a de <a href="#editor-highlight">realçar</a>.<br />
		<br><br />
		As opções disponíveis para ofuscação são:
	</p>
<ul>
<li><em>Pixelizar</em>: aumenta o tamanho do píxeis na área seleccionada</li>
<li><em>Desfocar</em><a href="#hint-blur">*</a>: desfoca a área seleccionada</li>
</ul>
<p>	<a name="hint-blur"></a></p>
<p class="hint">
		* Dependendo do desempenho do seu computador, a aplicação do efeito desfocar pode tornar lento<br />
		o editor de imagens do Greenshot. Se notar que o editor de imagem reage com lentidão logo que<br />
		o efeito é aplicado, tente reduzir o valor para a <em>Qualidade de Pré-visualização</em><br />
		na barra de ferramentas ou reduza o valor do <em>Raio de desfocagem</em>.<br><br />
		Se o desempenho da desfocar continuar mau para poder trabalhar com ele, deve optar por<br />
		utilizar o efeito Pixelizar.
	</p>
<p>	<a name="editor-effects"></a></p>
<h3>Adicionar efeitos</h3>
<p>
		Pode adicionar diferentes efeitos à sua captura. Pode querer aplicar uma borda, sombra ou efeito<br />
		de cantos arredondados, p.e. para visualizar em separado a sua captura do restante conteúdo do documento.<br />
		Os efeitos Escala Cinza e Inverter são úteis principalmente antes de imprimir, poupando tinta ou toner<br />
		ao imprimir capturas coloridas ou escuras.
	</p>
<p>	<a name="editor-crop"></a></p>
<h3>Recortar a Captura</h3>
<p>
		Se precisa apenas de uma parte da imagem que capturou, use a ferramenta de recorte <kbd>C</kbd><br />
		para recortá-la para a área desejada.<br><br />
		Após seleccionar a ferramenta recortar, desenhe um rectângulo na área da captura que quer<br />
		manter. Pode redimensionar a área seleccionada como qualquer outro elemento.<br><br />
		Quando estiver satisfeito com a sua selecção, use o botão confirmar da barra de ferramentas ou<br />
		prima a tecla <kbd>Enter</kbd>. Pode cancelar o recorte clicando no botão cancelar ou premindo<br />
		<kbd>ESC</kbd>.
	</p>
<p class="hint">
		<em>Recorte Automático</em>: Se necessita recortar uma borda de uma cor sólida de fundo da sua<br />
		captura escolha simplesmente <em>Recorte Automático</em> do menu <em>Editar</em> e o Greenshot<br />
		irá seleccionar automaticamente a área para recortar.
	</p>
<p>	<a name="editor-rotate"></a></p>
<p class="hint">
		Use as ferramentas de rotação para rodar a captura no sentido horário ou anti-horário.
	</p>
<p>	<a name="editor-adding-graphics"></a></p>
<h3>Adicionar gráficos a uma captura</h3>
<p>
		Pode muito simplesmente adicionar gráficos ou imagens à sua captura arrastando e largando um<br />
		ficheiro de imagem na janela do editor. Pode também inserir capturas de outras janelas seleccionando<br />
		<em>Inserir janela</em> do menu <em>Editar</em>. Surge uma lista com todas as janelas abertas,<br />
		permitindo-lhe seleccionar uma para ser inserida.
	</p>
<p>	<a name="editor-reuse-elements"></a></p>
<h3>Reutilizar elementos de desenho</h3>
<p>
		Se estiver a utilizar os mesmos ou elementos semelhentes na maioria das capturas<br />
		(p.e. um campo de texto contendo otipo de navegador e a versão, ou a ofuscar o mesmo<br />
		elemento em várias capturas) pode reutilizar os elementos.<br><br />
		Seleccione <em>Guardar objectos para ficheiro</em> do menu <em>Objectos</em> para<br />
		guardar o conjunto actual de elementos e reutilizá-los mais tarde. <em>Carregar objectos de ficheiro</em><br />
		aplica os mesmos elementos a outra captura.
	</p>
<p>	<a name="editor-export"></a></p>
<h3>Exportar a captura</h3>
<p>
		Após editar a captura, pode exportar o resultado para diversos fins, dependendo das<br />
		suas necessidades. Pode aceder a todas as opções de exportação através do menu <em>Ficheiro</em>,<br />
		da barra de ferramentas superior ou via teclas de atalho:
	</p>
<ul>
<li><em>Guardar</em> <kbd>Control</kbd> + <kbd>S</kbd>: guarda a imagem para um ficheiro (se a imagem já tiver sido guardada, exibe o diálogo <em>Guardar como...</em></li>
<li><em>Guardar como...</em> <kbd>Control</kbd> + <kbd>Shift</kbd> + <kbd>S</kbd>: deixa-o escolher a localização, nome do ficheiro e formato da imagem para o ficheiro a guardar</li>
<li><em>Copiar imagem para a área de transferência</em> <kbd>Control</kbd> + <kbd>Shift</kbd> + <kbd>C</kbd>: coloca uma cópia da imagem na área de transferência, permitindo colar em outros programas</li>
<li><em>Imprimir...</em> <kbd>Control</kbd> + <kbd>P</kbd>: envia a imagem para uma impressora</li>
<li><em>E-Mail</em> <kbd>Control</kbd> + <kbd>E</kbd>: abre uma nova mensagem no cliente de e-mail padrão, adicionando a imagem como anexo</li>
</ul>
<p>
		Existem plugins para exportar capturas para outros destinos, e.g. DropBox, Picasa, Flickr. Deve<br />
		seleccionar os plugins apropriados durante o processo de instalação.
	</p>
<p class="hint">
		Após guardar uma imagem do editor, clique com o botão direito do rato na barra de estado<br />
		na parte inferior da janela do editor para copiar o atalho do ficheiro para a área de transferência<br />
		ou abrir a pasta respectiva no Explorador do Windows.
	</p>
<p>	<a name="settings"></a></p>
<h2>O Diálogo Definições</h2>
<p>	<a name="settings-general"></a></p>
<h3>Definições Gerais</h3>
<ul>
<li><em>Idioma</em>: O idioma preferido a utilizar.<br><br />
			Pode transferir ficheiros de idiomas adicionais para o Greenshot <a target="_blank" href="/downloads/">aqui</a>. </li>
<li><em>Iniciar o Greenshot no arranque</em>: Inicia o programa no arranque do sistema.</li>
<li><em>Teclas de Atalho</em>: Personaliza as teclas de atalho a serem usadas para criar capturas.</li>
<li><em>Usar proxy padrão do sistema</em>: Se seleccionado, o Greenshot usa proxy padrão do sistema para procurar actualizações.</li>
<li><em>Intervalo de procura de actualizações em dias</em>: O Greenshot pode procurar actualizações automaticamente. Use esta definição para ajustar o<br />
			intervalo (em dias) ou defina-o para 0 para desligar a procura de actualizações.</li>
</ul>
<p>	<a name="settings-capture"></a></p>
<h3>Definições de Captura</h3>
<ul>
<li><em>Capturar cursor do rato</em>: Se seleccionada, o cursor do rato será capturado. O cursor é tratado no editor como um elemento separado, por isso pode movê-lo ou removê-lo mais tarde.</li>
<li><em>Reproduzir som de Câmara</em>: Som audível ao efectuar a captura</li>
<li><em>Milisegundos de espera antes da captura</em>: Adiciona um tempo de atraso personalizado antes de capturar realmente o ecrã.</li>
<li><em>Usar modo de captura janela interactiva</em>: Em vez de capturar a janela activa de imediato, o modo interactivo permite-lhe<br />
			seleccionar a janela a capturar. Também é possível capturar janelas secundárias, ver <a href="#capture-window">captura de janela</a>.</li>
<li>
			<em>Captura estilo Aero (apenas Windows Vista / 7)</em>: Se estiver a usar o Greenshot no Windows Vista ou Windows 7 com janelas estilo aero activado, pode<br />
			escolher como serão tratadas as bordas transparentes da janela ao criar uma captura em modo janela. Use esta definição para evitar capturar elementos do<br />
			fundo a brilhar através das bordas transparentes.</p>
<ul>
<li><em>Auto</em>: Deixa o Greenshot decidir como tratar a transparência.</li>
<li><em>Como exibida</em>: As bordas transparentes são capturadas tal como exibidas no ecrã.</li>
<li><em>Usar cor padrão</em>: É aplicada uma cor sólida padrão em vez da transparência.</li>
<li><em>Usar cor personalizada</em>: Seleccione uma cor sólida para ser aplicada em vez da transparência.</li>
<li><em>Preservar transparência</em>: As bordas são capturadas mantendo a transparência, não capturando elementos que possam estar no fundo. (Nota: as áreas<br />
					transparentes serão exibidas usando um padrão seleccionado no editor. O padrão não é exportado ao guardar a captura para um ficheiro. Lembre-se de guardar<br />
					como ficheiro PNG para suporte completo da transparência.)</li>
</ul>
</li>
<li><em>Capturar Internet Explorer</em>: Permite a captura confortável de página web com o Internet Explorer.</li>
<li><em>Redimensionar a janela do editor ao tamanho da captura</em>: Se seleccionado, a janela do editor será redimensionada automaticamente para o tamanho da captura.</li>
</ul>
<p>	<a name="settings-output"></a></p>
<h3>Definições de Saída</h3>
<ul>
<li><em>Destino da captura</em>: Permite-lhe escolher o(s) destino(s) para a sua captura imediatamente após efectuá-la.</li>
<li><em>Definições preferidas do ficheiro de saída</em>: Pasta e nome de ficheiro a ser usado ao guardar directamente ou para ser sugerido ao guardar (usando o diálogo Guardar como). Clique no botão <em>?</em> para aprender mais acerca dos marcadores que podem ser usados como padrão de nome de ficheiro.</li>
<li><em>Definições JPEG</em>: Qualidade a ser usada ao guardar ficheiros JPEG</li>
</ul>
<p>	<a name="settings-destination"></a></p>
<h3>Definições de Destino</h3>
<p>
		Por defeito, o Greenshot deixa-o seleccionar dinamicamente um destino após criar a captura, exibindo um pequeno<br />
		menu com diferentes destinos à sua escolha. Se não quer ou não necessita de estar sempre a alterar os destinos,<br />
		pode preferir configurar o Greenshot para exportar directamente para um ou mais destinos, sem exibir o diálogo de<br />
		selecção dos destinos.<br/><br />
		Nota: tal como <a href="#editor-export">exportar da janela do editor, os destinos exibidos variam<br />
		dependendo dos plugins que tem instalados com o Greenshot.</a>
	</p>
<p>	<a name="settings-printer"></a></p>
<h3>Definições de impressão</h3>
<ul>
<li><em>Reduzir impressão para caber no tamanho do papel</em>: Se a imagem exceder o tamanho do papel, será reduzida para caber na página.</li>
<li><em>Ampliar impressão para caber no tamanho do papel</em>: Se a imagem for mais pequena do que o tamanho do papel, será dimensionada para ser impressa tão grande quanto possível sem exceder o tamanho do papel.</li>
<li><em>Rodar impressão para orientação da página</em>: A imagem de formato paisagem será rodada 90&deg; para impressão.</li>
<li><em>Imprimir com cores invertidas</em>: A captura será invertida antes de ser impressa, útil p.e. ao imprimir uma captura de texto branco em fundo preto (para poupar toner/tinta).</li>
<li><em>Forçar impressão em escala cinza</em>: As cores serão convertidas para a escala cinza antes de imprimir.</li>
</ul>
<p>	<a name="settings-plugins"></a></p>
<h3>Definições de Plugins</h3>
<p>
		Exibe uma lista de plugins do Greenshot instalados. Seleccione um da lista e clique <em>Configurar</em> com vista a aceder<br />
		à configuração de um plugin.
	</p>
<p>	<a name="settings-expert"></a></p>
<h3>Definições avançadas</h3>
<p>
		Queremos dizer: é melhor não alterar nada aqui se não souber o que está a fazer, pois estas definições podem provocar comportamentos inesperados.
	</p>
<p>	<a name="help"></a></p>
<h2>Quer Ajudar?</h2>
<p>
		De momento, não precisamos de ajuda no desenvolvimento. Contudo, há várias coisas que<br />
		pode fazer para ajudar o Greenshot e a equipa de desenvolvimento.<br><br />
		Antecipadamente gratos :)
	</p>
<p>	<a name="help-donate"></a></p>
<h3>Considere um donativo</h3>
<p>
		Estamos a investir muito trabalho no Greenshot e a gastar bastante tempo para fornecer<br />
		uma boa peça de software gratuíto e de código aberto. Se acha que<br />
		isso o torna mais produtivo, e lhe poupa (ou à sua empresa)<br />
		muito tempo e dinheiro, ou se simplesmente gosta do Greenshot e<br />
		da ideia de sotware de código aberto: por favor considere compensar o nosso esforço, doando.<br><br />
		Por favor visite a nossa página web para ver como pode apoiar a equipa de desenvolvimento do Greenshot:<br><br />
		<a target="_blank" href="/support/">http://getgreenshot.org/support/</a>
	</p>
<p>	<a name="help-spread"></a></p>
<h3>Passe a palavra</h3>
<p>
		Se gosta do Greenshot, deixe que as pessoas saibam: Fale sobre o Greenshot aos seus amigos e colegas.<br />
		Os seus seguidores, também :)<br><br />
		Classifique o Greenshot nos portais de software ou tenha um link da nossa página no seu blog ou site.
	</p>
<p>	<a name="help-translate"></a></p>
<h3>Envie uma tradução</h3>
<p>
		O Greenshot não está disponível no seu idioma preferido? Se acha que pode traduzir<br />
		uma peça de software, é mais do que bem-vindo.<br />
		Se é um utilizador registado no sourceforge.net, pode enviar traduções para o nosso<br />
		<a target="_blank" href="https://sourceforge.net/tracker/?group_id=191585&atid=1368020">monitor de traduções</a>.<br><br />
		Por favor certifique-se de que não existe tradução para o seu idioma na nossa<br />
		<a target="_blank" href="/downloads/">página de transferências</a>. Veja também no nosso <a href="https://sourceforge.net/tracker/?group_id=191585&atid=1368020">monitor de traduções</a>,<br />
		se existe alguma tradução a decorrer, ou pelo menos em discussão.<br><br />
		Tenha em atenção que só disponibilizaremos uma tradução na nossa página de transferências se ela<br />
		tiver sido enviada através da sua conta no sourceforge.net. Uma vez que o mais provável é nós<br />
		não sermos capazes de perceber a sua tradução, é bom que outros utilizadores do sourceforge<br />
		tenham possibilidade de contactá-lo acerca de aperfeiçoamentos no caso de nova versão do Greenshot.
	</p>
