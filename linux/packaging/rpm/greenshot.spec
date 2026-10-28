# Greenshot for Linux (Wayland). The source is made with linux/packaging/source-tarball.sh.
# The build restores NuGet packages, so it needs network access (COPR: enable "networking" for the chroots).
%global debug_package %{nil}
%global __requires_exclude_from ^%{_libdir}/greenshot/.*$
%global __provides_exclude_from ^%{_libdir}/greenshot/.*$

Name:           greenshot
Version:        1.4.0
Release:        1%{?dist}
Summary:        Take screenshots, annotate them and send them where they belong
License:        GPL-3.0-or-later
URL:            https://getgreenshot.org/
Source0:        %{name}-%{version}.tar.gz
ExclusiveArch:  x86_64 aarch64

BuildRequires:  dotnet-sdk-10.0
BuildRequires:  desktop-file-utils
BuildRequires:  appstream
Requires:       dotnet-runtime-10.0
Requires:       wl-clipboard
Requires:       hicolor-icon-theme
Recommends:     grim
Recommends:     xdg-desktop-portal
Recommends:     tesseract
Suggests:       tesseract-langpack-deu

%description
Greenshot captures a region, a window or the whole screen of a Wayland desktop (Sway, Hyprland and other wlroots
compositors, GNOME, KDE), lets you annotate, highlight or obfuscate parts of it and sends it on: to a file, the
clipboard, the printer, an e-mail, Box, Dropbox, Jira, Confluence or your own uploader. It reads text (OCR) and QR
codes, automates captures with recipes, and has a command line and an MCP server for AI tools.

%prep
%autosetup

%build
# Built in %%install by the staging script, which publishes and places the files

%install
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
linux/packaging/stage.sh --prefix %{_prefix} --libdir %{_lib} --destdir %{buildroot}

%check
desktop-file-validate %{buildroot}%{_datadir}/applications/org.getgreenshot.Greenshot.desktop
desktop-file-validate %{buildroot}%{_datadir}/applications/org.getgreenshot.Greenshot.Url.desktop
appstreamcli validate --no-net %{buildroot}%{_metainfodir}/org.getgreenshot.Greenshot.metainfo.xml

%files
%license LICENSE
%doc linux/README.md
%{_bindir}/greenshot
%{_libdir}/greenshot/
%{_datadir}/applications/org.getgreenshot.Greenshot.desktop
%{_datadir}/applications/org.getgreenshot.Greenshot.Url.desktop
%{_metainfodir}/org.getgreenshot.Greenshot.metainfo.xml
%{_datadir}/icons/hicolor/*/apps/org.getgreenshot.Greenshot.*

%changelog
* Wed Oct 28 2026 Greenshot - 1.4.0-1
- The Linux version for Wayland desktops
