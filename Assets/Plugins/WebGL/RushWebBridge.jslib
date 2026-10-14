mergeInto(LibraryManager.library, {
    RushGetUrlQueryParam: function(paramNamePtr) {
        var paramName = UTF8ToString(paramNamePtr);
        var urlParams = new URLSearchParams(window.location.search);
        var val = urlParams.get(paramName);
        if (!val) return 0;
        var bufferSize = lengthBytesUTF8(val) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(val, buffer, bufferSize);
        return buffer;
    },
    RushCopyTextToClipboard: function(textPtr) {
        var str = UTF8ToString(textPtr);
        if (navigator.clipboard && navigator.clipboard.writeText) {
            navigator.clipboard.writeText(str).catch(function(err) {
                console.error("Clipboard copy failed: ", err);
            });
        } else {
            var el = document.createElement('textarea');
            el.value = str;
            el.style.position = 'fixed';
            el.style.opacity = '0';
            document.body.appendChild(el);
            el.focus();
            el.select();
            try {
                document.execCommand('copy');
            } catch (e) {}
            document.body.removeChild(el);
        }
    }
});
