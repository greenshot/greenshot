---
layout: page
status: publish
published: true
title: Greenshot Yardımı
author:
  display_name: greenshot
  login: admin
  email: greenshot-developers@lists.sourceforge.net
  url: http://getgreenshot.org/
author_login: admin
author_email: greenshot-developers@lists.sourceforge.net
author_url: http://getgreenshot.org/
wordpress_id: 398
wordpress_url: http://getgreenshot.org/
date: !binary |-
  MjAxMi0wNC0wOSAwODo0NDoxNyArMDIwMA==
date_gmt: !binary |-
  MjAxMi0wNC0wOSAwNjo0NDoxNyArMDIwMA==
categories: []
tags: []
comments: []
---
<p><small>Version 0.8 Yardım içeriğini Türkçe'ye çeviren Kaya Zeren</small></p>
<h2>İçindekiler</h2>
<ol>
<li><a href="#screenshot">Bir ekran görüntüsünü yakalamak</a></li>
<ol>
<li><a href="#capture-region">Yakalanacak bölge</a></li>
<li><a href="#capture-last-region">Son bölgeyi yakala</a></li>
<li><a href="#capture-window">Pencereyi yakala</a></li>
<li><a href="#capture-fullscreen">Tüm ekranı yakala</a></li>
</ol>
<li><a href="#editor">Görüntü düzenleyicinin kullanımı</a></li>
<ol>
<li><a href="#editor-shapes">Şekil çizilmesi</a></li>
<li><a href="#editor-text">Yazı eklenmesi</a></li>
<li><a href="#editor-highlight">Nesnelerin vurgulanması</a></li>
<li><a href="#editor-obfuscate">Obfuscating things</a></li>
<li><a href="#editor-crop">Ekran görüntüsünün traşlanması</a></li>
<li><a href="#editor-reuse-elements">Re-using drawn elements</a></li>
<li><a href="#editor-export">Ekran görüntüsünün verilmesi</a></li>
</ol>
<li><a href="#settings">Ayarlar penceresi</a></li>
<ol>
<li><a href="#settings-general">Genel ayarlar</a></li>
<li><a href="#settings-output">Dosya ayarları</a></li>
<li><a href="#settings-printer">Yazıcı ayarları</a></li>
</ol>
<li><a href="#help">Yardımcı olmak ister misiniz?</a></li>
<ol>
<li><a href="#help-donate">Bağış yapmayı düşünün</a></li>
<li><a href="#help-spread">Dünyaya yayın</a></li>
<li><a href="#help-translate">Kendi dilinize çevirin</a></li>
</ol>
</ol>
<p>	<a name="screenshot"></a></p>
<h2>Bir ekran görüntüsü yakalamak</h2>
<p>
		Bir ekran görüntüsünü klavyenizdeki <kbd>Print</kbd> tuşuna basarak<br />
		veya sistem tepsisindeki Greenshot simgesine sağ tıklayarak yakalayabilirsiniz.<br><br />
		Bir kaç farklı ekran görüntüsü yakalama yöntemi vardır:
	</p>
<p>	<a name="capture-region"></a></p>
<h3>Bölge yakalama <kbd>Print</kbd></h3>
<p>
		Bölge yakalama kipi ekranın bir bölümünü işaretleyip yakalamanızı sağlar.<br><br />
		Bölge kipini seçtikten sonra fare imlecinin yerinde bir artı işareti görürsünüz.<br />
		Bu işareti yakalamak istediğiniz ekran bölümünün bir köşesine tıklayıp basılı<br />
		tutun ve diğer köşeye çekerek bir dikdörtgen çizin. İstediğiniz bölgeyi seçtiğinizde<br />
		fare tuşunu bırakın.
	</p>
<p class="hint">
		   Klavyedeki <kbd>Boşluk</kbd> tuşunu kullanarak bölge ve<br />
		<a href="#capture-window">pencere</a> yakalama kipi arasında geçiş yapabilirsiniz.
	</p>
<p class="hint">
		Kesin bir bölgeyi yakalamak istiyorsanız biraz daha geniş bir alanı seçip yakaladıktan<br />
		sonra Greenshot görüntü düzenleyicide <a href="#editor-crop">kırpma</a> işlemini yapmak<br />
		kolaylık sağlar.
	</p>
<p>	<a name="capture-last-region"></a></p>
<h3>Son bölgeyi yakala <kbd>Shift</kbd> + <kbd>Print</kbd></h3>
<p>
		Eğer daha önce bir <a href="#capture-region">bölge yakalama</a> veya<br />
		<a href="#capture-window">pencere yakalama</a> işlemi yaptıysanız aynı bölgeyi<br />
		yeniden yakalamak için bu seçeneği kullanın.
	</p>
<p>	<a name="capture-window"></a></p>
<h3>Pencere yakala <kbd>Alt</kbd> + <kbd>Print</kbd></h3>
<p>
		Etkin pencerenin ekran görüntüsünü yakalar.
	</p>
<p class="hint">
		<a href="#settings">Ayarlar</a> bölümünden doğrudan etkin pencereyi<br />
		yakalamayı veya etkileşimli olarak istediğiniz pencereye tıklayarak<br />
		yakalamayı seçebilirsiniz. Bu seçenek seçildiğinde,<br />
		<a href="#capture-region">bölge kipindeki</a> gibi bir pencereyi tıklayarak<br />
		seçebilirsiniz. Greenshot yakalanacak bölgeyi vurgulayacaktır.<br><br />
		Eğer yakalanmasını istediğiniz bir alt pencereyse (araç çubukları olmadan bir<br />
		tarayıcı görünümü, veya bir web sayfasının bir çerçevesi gibi) fare imlecini<br />
		pencerenin üzerine getirip <kbd>PgDown</kbd> tuşuna basın. Bunu yaptıktan sonra<br />
		pencerenin alt ögelerini seçerek yakalayabilirsiniz.
	</p>
<p>	<a name="capture-fullscreen"></a></p>
<h3>Tüm ekranı yakala <kbd>Control</kbd> + <kbd>Print</kbd></h3>
<p>
		Ekrandaki tüm görüntüyü yakalar.
	</p>
<p>	<a name="editor"></a></p>
<h2>Görüntü düzenleyiciyi kullanmak</h2>
<p>
		Greenshot kolay kullanılan, not ve şekil ekleme özellikleri olan<br />
		bir görüntü düzenleyici ile birlikte gelir. Ayrıca ekran görüntünüzün<br />
		vurgulanması veya bulanıklaştırılmasını sağlayan işlevleri vardır.
	</p>
<p class="hint">
		Greenshot görüntü düzenleyici yalnızca ekran görüntüleri için kullanılmayabilir.<br />
		Başka resim dosyaları ve panoya kopyalanmış resimleri de düzenleyebilirsiniz.<br />
		Bunu yapmak için sistem tepsisindeki Greenshot simgesine sağ tıklayın ve<br />
		<em>Dosyadaki görüntüyü aç</em> veya <em>Panodaki görüntüyü aç</em> komutunu seçin.
	</p>
<p class="hint">
		Varsayılan olarak, görüntü düzenleyici, bir ekran görüntüsü yakalandığında açılır<br />
		Eğer görüntü düzenleyiciyi kullanmak istemiyorsanız <a href="#settings">ayarlar</a><br />
		bölümünden devre dışı bırakabilirsiniz.
	</p>
<p>	<a name="editor-shapes"></a></p>
<h3>Şekillerin çizilmesi</h3>
<p>
		Görüntü düzenleyicinin sol tarafındaki araç çubuğundan veya <em>Nesne</em> menüsünden<br />
		şekil çizme araçlarından birini seçin. Ayrıca daha rahat kullanabilmek için her bir<br />
		araca bir tuş atanmıştır.<br><br />
		Kullanılabilecek şekiller: dikdörtgen <kbd>R</kbd>, elips <kbd>E</kbd>, çizgi <kbd>L</kbd><br />
		ve oktur <kbd>A</kbd>.<br><br />
		Fare tuşuna basılı tutup sürükleyerek şekilin konumu ve boyutunu belirleyebilirsiniz.<br />
		Şekli çizmeyi bitirdiğinizde fare tuşunu bırakın.<br />
		Click, hold down the mouse button and drag to define position and size of the shape.<br />
		Release the mouse button when you are done.
	</p>
<p>
		Varolan şekilleri taşımak ve yeniden boyutlandırmak için araç çubuğundaki<br />
		seçme aracını <kbd>ESC</kbd> kullanabilirsiniz.<br><br />
		Her şekil tipi için, çizgi kalınlığı, çizgi rengi, arkaplan rengi gibi değiştirebileceğiniz<br />
		farklı özellikler vardır. Varolan bir şekli seçtikten sonra ayarlarını değiştirebilirsiniz.<br />
		Yaptığınız ayarlar kalıcı olur ve daha sonra çizeceğiniz aynı tipteki şekiller bu ayarlarla<br />
		çizilir.
	</p>
<p class="hint">
		Aynı anda bir kaç şekli de düzenleyebilirsiniz. Birden fazla şekli seçmek için<br />
		<kbd>Harf kaydırma (Shift)</kbd> tuşuna basılı tutarken şekillere tıklayın.
	</p>
<p>	<a name="editor-text"></a></p>
<h3>Yazı ekleme</h3>
<p>
		Yazı aracının <kbd>T</kbd> kullanımı <a href="#editor-shapes">şekil</a> araçlarına çok benzer.<br />
		İstediğiniz boyda bir yazı nesnesi çizin ve yazıyı yazın.<br><br />
		Varolan bir yazı nesnesini düzenlemek için üzerinde çift tıklayın.
	</p>
<p>	<a name="editor-highlight"></a></p>
<h3>Bölgelerin vurgulanması</h3>
<p>
		Vurgulama aracını seçtikten sonra <kbd>H</kbd>, vurgulamak istediğiniz alanı bir<br />
		<a href="#editor-shapes">sekil</a> çizer gibi belirleyebilirsiniz.<br><br />
		Vurgulanacak alanın nasıl gösterileceği ile ilgili bir kaç seçeneğiniz vardır.<br />
		Bunları üst araç çubuğunun en solundaki düğmeye tıklayarak görebilirsiniz. :
	</p>
<ul>
<li><em>Yazıyı vurgula</em>: Bir bölgeyi fosforlu bir kalemle işaretlenmiş gibi parlak bir<br />
		renkle işaretler</li>
<li><em>Bölgeyi vurgula</em>: Seçili alanın dışındaki her şeyi bulanıklaştırır<a href="#hint-blur">*</a> ve karartır</li>
<li><em>Gritonlama</em>: Seçili alanın dışındaki her şey gritonlanır</li>
<li><em>Büyüt</em>: Seçili alan büyütülmüş olarak gösterilir</li>
</ul>
<p>	<a name="editor-obfuscate"></a></p>
<h3>Karartma</h3>
<p>
		Ekran görüntüsünde ad, parola, resim gibi başkalarının görmesini istemediğiniz bilgiler varsa bunları<br />
		karartmak iyi bir fikirdir.<br><br />
		Karartma aracını <kbd>O</kbd> <a href="#editor-highlight">vurgulama</a> aracı ile aynı şekilde<br />
		kullanabilirsiniz.<br><br />
		Karatma için kullanabileceğiniz seçenekler şunlardır:
	</p>
<ul>
<li><em>Pikselleştir</em>: Seçili alandaki piksel boyutunu büyütür</li>
<li><em>Bulanıklaştır</em><a href="#hint-blur">*</a>: Seçili alanı bulanıklaştırır</li>
</ul>
<p>	<a name="hint-blur"></a></p>
<p class="hint">
		* Bilgisayarınızın başarımına bağlı olarak bulanıklaştırma işlemi Greenshot görüntü<br />
		düzenleyiciyi yavaşlatabilir. Bulanıklaştırmayı uygular uygulamaz görüntü düzenleyicinin<br />
		yavaşladığını hissederseniz, araç çubuğundan <em>Önizleme kalitesi</em><br />
		veya <em>Bulanıklaştırma çapı</em> değerini küçültün.<br><br />
		Başarım hala çalışılamayacak kadar kötüyse, bulanıklaştırma yerine pikselleştirme<br />
		etkisini kullanabilirsiniz.
	</p>
<p>	<a name="editor-crop"></a></p>
<h3>Ekran görüntüsünün kırpılması</h3>
<p>
		Yakaladığınız ekran görüntüsünün yalnızca bir bölümünü kullanacaksanız, istediğiniz<br />
		bölgeyi kırpmak için kırpma aracını	<kbd>C</kbd> kullanın.<br />
		Kırpma aracını seçtikten sonra elinizde kalmasını istediğiniz bölümü bir dikdörtgen<br />
		çizerek seçin. Seçtiğiniz bölgeyi diğer nesnelerde olduğu gibi yeniden<br />
		boyutlandırabilirsiniz.<br><br />
		Seçiminizi tamamladığınızda üst araç çubuğunda soldaki onaylama düğmesine tıklayın ya da<br />
		<kbd>Enter</kbd> tuşuna basın. Kırpma işlemini iptal etmek için  iptal düğmesine tıklayın<br />
		ya da <kbd>ESC</kbd> tuşuna basın.
	</p>
<p>	<a name="editor-reuse-elements"></a></p>
<h3>Çizilmiş şekillerin yeniden kullanılması</h3>
<p>
		Ekran görüntülerinizde sık sık aynı veya benzer şekilleri kullanıyorsanız (tarayıcı tipi<br />
		ve sürümünün yazdığı bir yazı alanı veya bir kaç ekran görüntüsünde bulanıklaştırılmış aynı<br />
		şekiller gibi) nesneleri yeniden kullanabilirsiniz.<br><br />
		<em>Nesne</em> menüsünden <em>Nesneleri dosyaya kaydet</em> seçeneğini seçerek kullandığınız<br />
		nesne takımını daha sonra yine kullanabilmek için kaydedebilirsiniz.<br />
		<em>Dosyadan nesneleri yükle</em> seçeneği ile aynı nesneleri başka bir ekran görüntüsüne<br />
		uygulayabilirsiniz.
	</p>
<p>	<a name="editor-export"></a></p>
<h3>Ekran görüntüsünün aktarılması</h3>
<p>
		Ekran görüntüsünü düzenledikten sonra sonucu farklı amaçlarla aktarmak isteyebilirsiniz.<br />
		Tüm aktarma seçeneklerini <em>Dosya</em> menüsünden, en üstteki araç çubuğundan veya<br />
		kısayol tularından ulaşabilirsiniz.
	</p>
<ul>
<li><em>Kaydet</em> <kbd>Control</kbd> + <kbd>S</kbd>: Görüntüyü bir dosyaya kaydeder(eğer görüntü zaten kaydedildiyse <em>Farklı kaydet...</em> penceresini gösterir</li>
<li><em>Farklı kaydet...</em> <kbd>Control</kbd> + <kbd>Shift</kbd> + <kbd>S</kbd>: Kaydedilecek dosyanın yeri, adı ve görüntü biçimi </li>
<li><em>Görüntüyü panoya kopyala</em> <kbd>Control</kbd> + <kbd>Shift</kbd> + <kbd>C</kbd>: Görüntünün bir kopyasını panoya alır. Böylece ekran görüntüsünü başka programlara yapıştırabilirsiniz</li>
<li><em>Yazdır...</em> <kbd>Control</kbd> + <kbd>P</kbd>: Görüntüyü yazıcıya gönderir</li>
<li><em>E-posta</em> <kbd>Control</kbd> + <kbd>E</kbd>: Varsayılan e-posta programınızı açarak, gönderilmek üzere görüntünün ekli olduğu yeni bir ileti oluşturur</li>
</ul>
<p class="hint">
		Düzenleyicideki bir görüntüyü kaydettikten sonra alttaki durum çubuğuna sağ tıklayarak,<br />
		dosya yolunu panoya kopyalayabilir ya da dosyanın kopyalandığı klasörü windows gezgininde<br />
		açabilirsiniz.
	</p>
<p>	<a name="settings"></a></p>
<h2>Ayarlar penceresi</h2>
<p>	<a name="settings-general"></a></p>
<h3>Genel ayarlar</h3>
<ul>
<li><em>Dil</em>: Programı kullanmayı tercih ettiğiniz dil.<br><br />
			Greenshot için kullanılabilecek diğer dilleri <a target="_blank" href="http://getgreenshot.org/downloads/">buradan indirebilirsiniz</a>. </li>
<li><em>Kısayol tuşlarını devral</em>: İşaretlediğinizde, Greenshot <kbd>Print</kbd> tuşu ile kullanılabilir.</li>
<li><em>Windows başlangıcında çalıştır</em>: Bilgisayar başlatıldığında programı çalıştır.</li>
<li><em>Flaş etkisi</em>: Ekran yakalarken görsel flaş çakması etkisi oluşturur</li>
<li><em>Kamera sesi</em>: Ekran yakalarken işitsel kamera sesi etkisi oluşturur</li>
<li><em>Fare imlecini de yakala</em>: İşaretlediğinizde fare imleci de yakalanır. İmleç görüntü düzenleyicide ayrı bir nesne olarak görüntülenir. Böylece daha sonra taşıyabilir ya da silebilirsiniz.</li>
<li><em>Etkileşimli pencere yakalama kipini kullan</em>: Etkileşimli kip, doğrudan etkin ekranı yakalamak yerine, yakalamak istediğiniz pencereyi seçmenize olanak tanır. Bu şekilde ayrıca alt pencereler de yakalanabilir. Ayrıntılar için <a href="#capture-window">pancere yakala</a> bölümüne bakın.</li>
</ul>
<p>	<a name="settings-output"></a></p>
<h3>Çıkış ayarları</h3>
<ul>
<li><em>Yakalanan ekran görüntüsünün hedefi</em>: Ekran görüntüsü yakalandıktan sonra bir ya da bir kaç hedefe gönderebilirsiniz.</li>
<li><em>Çıkış dosyası ayarları</em>: Doğrudan kaydederken veya farklı kaydet penceresinde gösterilecek klasör ve dosya adı. <em>?</em> düğmesine tıklayarak dosya adı biçiminde kullanılabilen ifadeler hakkında ayrıntılı bilgi alabilirsiniz.</li>
<li><em>JPEG ayarları</em>: JPEG dosyaları kaydedilirken kullanılacak kalite ayarları</li>
</ul>
<p>	<a name="settings-printer"></a></p>
<h3>Yazıcı ayarları</h3>
<ul>
<li><em>Sayfaya sığacak şekilde daralt</em>: Eğer görüntü kağıt boyutundan taşıyorsa sayfaya sığacak şekilde daraltılır.</li>
<li><em>Sayfaya sığacak şekilde genişlet</em>: Eğer görüntü kağıt boyutundan küçükse sayfaya sığacak şekilde büyütülür</li>
<li><em>Görüntüyü sayfa duruşuna göre döndür</em>: Yatay biçimli bir görüntüyü yazdırmak için 90 derece döndürür.</li>
</ul>
<p>	<a name="help"></a></p>
<h2>Yardım etmek mi istiyorsunuz?</h2>
<p>
		Şu anda geliştirme içinb yardıma gerek yok. Ancak Greenshot ve geliştirme takımını desteklemek<br />
		için yapabileceğiniz bir kaç şey var.<br><br />
		Teşekkürler :)
	</p>
<p>	<a name="help-donate"></a></p>
<h3>Bağış yapmayı düşünün</h3>
<p>
		Greenshot gibi iyi bir yazılımı ücretsiz ve açık kaynaklı olarak sunmak için<br />
		oldukça çok emek harcıyoruz. Üretkenliğinize katkıda bulunduğuna inanıyorsanız, size<br />
		veya kurumunuza para ve zaman kazandırıyorsa ya da Greenshot ve açık kaynak felsefesinden<br />
		hoşlanıyorsanız lütfen bağış yaparak emeklerimize saygı gösterin.<br><br />
		Greenshot geliştirici takımını nasıl destekleyeceğinizi görmek için web sitemize bakın:<br><br />
		<a target="_blank" href="http://getgreenshot.org/support/">http://getgreenshot.org/support/</a>
	</p>
<p>	<a name="help-spread"></a></p>
<h3>Yayılmasına yardım edin</h3>
<p>
		Greenshot hoşunuza gittiyse, tanıdıklarınıza anlatın ve onların da yararlanmasını sağlayın.<br><br />
		Yazılım yorumlama sitelerinde bahsedin ve blogunuz veya web sitenizden bağlantı verin.
	</p>
<p>	<a name="help-translate"></a></p>
<h3>Kendi dilinize çevirin</h3>
<p>
		Greenshot sizin dilinizi konuşmuyor mu? Eğer bir yazılımı çevirebileceğinizi düşünüyorsanız<br />
		hoşgeldinden fazlasını hakediyorsunuz.<br />
		Eğer kayıtlı bir sourceforge.net üyesi iseniz, çevirilerinizi<br />
		<a target="_blank" href="https://sourceforge.net/tracker/?group_id=191585&atid=1368020">çeviri izleyicimize</a> gönderbilirsiniz.<br><br />
		Öncesinde<br />
		<a target="_blank" href="http://getgreenshot.org/downloads/">indirme sayfasından</a> dilinizde bir çeviri olmadığından emin olun. Ayrıca <a target="_blank" href="https://sourceforge.net/tracker/?group_id=191585&atid=1368020">çeviri izleyicimizden</a> de durumu denetleyin.<br />
		Dilinizde bir çeviri çalışması planlanıyor veya yapılıyor olabilir.<br><br />
		Çevirileri indirme sayfalarımızda ancak sourceforge.net kullanıcı hesabınızla gönderdiyseniz<br />
		yayınlayacağız. Böylece bizler çevirinizi büyük olasılıkla anlayamayacağımız için diğer sourceforge.<br />
		net kullanıcılarının sizinle iletişim kurması veya yeni sürümlerin çevirisini güncellenmek için<br />
		devreye girebilmesi sağlanacak.
	</p>
