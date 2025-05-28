mergeInto(LibraryManager.library, {
  IsMobileBrowser: function () {
    return (/iPhone|iPad|iPod|Android/i.test(navigator.userAgent));
  },
  IsPreferredDesktopPlatform: function() {
    return (/Win64|Mac OS X|Linux x86_64/i.test(navigator.userAgent));
  }
  });