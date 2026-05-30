mergeInto(LibraryManager.library, {
  IsMobileBrowser: function () {
    return (/iPhone|iPad|iPod|Android/i.test(navigator.userAgent));
  },
  IsTabletBrowser: function () {
    var ua = navigator.userAgent;
    if (/iPad/i.test(ua)) return true;
    if (navigator.platform === "MacIntel" && navigator.maxTouchPoints > 1) return true;
    if (/Android/i.test(ua) && !/Mobile/i.test(ua)) return true;
    return false;
  },
  IsPreferredDesktopPlatform: function() {
    return (/Win64|Mac OS X|Linux x86_64/i.test(navigator.userAgent));
  }
  });