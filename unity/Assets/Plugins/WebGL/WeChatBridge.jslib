mergeInto(LibraryManager.library, {
  WxShare: function (titlePtr) {
    var title = UTF8ToString(titlePtr);
    if (typeof wx !== 'undefined' && wx.shareAppMessage) {
      wx.shareAppMessage({ title: title });
    } else {
      console.log('WxShare', title);
    }
  },
  WxRegisterLifecycleCallbacks: function (targetPtr) {
    var target = UTF8ToString(targetPtr);
    var state = globalThis.__PocketCityWeChatBridgeLifecycle || {
      registered: false,
      target: '',
      hideCount: 0,
      showCount: 0,
    };

    if (state.registered && state.target === target) {
      return;
    }

    var sendLifecycleMessage = function (method) {
      try {
        if (typeof SendMessage === 'function') {
          SendMessage(target, method, '');
        } else if (typeof Module !== 'undefined' && typeof Module.SendMessage === 'function') {
          Module.SendMessage(target, method, '');
        } else {
          console.log('WxRegisterLifecycleCallbacks', target, method);
        }
      } catch (error) {
        console.warn('WxRegisterLifecycleCallbacks SendMessage failed', error);
      }
    };

    state.registered = true;
    state.target = target;
    globalThis.__PocketCityWeChatBridgeLifecycle = state;

    try {
      if (typeof wx !== 'undefined' && wx.onHide && wx.onShow) {
        wx.onHide(function () {
          state.hideCount += 1;
          sendLifecycleMessage('OnWeChatHide');
        });
        wx.onShow(function () {
          state.showCount += 1;
          sendLifecycleMessage('OnWeChatShow');
        });
      } else {
        console.log('WxRegisterLifecycleCallbacks', target);
      }
    } catch (error) {
      state.registered = false;
      console.warn('WxRegisterLifecycleCallbacks failed', error);
    }
  },
  WxVibrateShort: function (reasonPtr) {
    var reason = reasonPtr ? UTF8ToString(reasonPtr) : '';
    var feedbackType = 'light';
    if (reason === 'success') {
      feedbackType = 'medium';
    } else if (reason === 'warning') {
      feedbackType = 'heavy';
    }

    try {
      if (typeof wx !== 'undefined' && wx.vibrateShort) {
        wx.vibrateShort({ type: feedbackType });
      } else {
        console.log('WxVibrateShort', reason, feedbackType);
      }
    } catch (error) {
      console.warn('WxVibrateShort failed', error);
    }
  },
  WxSetStorageString: function (keyPtr, valuePtr) {
    var key = UTF8ToString(keyPtr);
    var value = UTF8ToString(valuePtr);
    try {
      if (typeof wx !== 'undefined' && wx.setStorageSync) {
        wx.setStorageSync(key, value);
      } else {
        localStorage.setItem(key, value);
      }
      return 1;
    } catch (error) {
      console.warn('WxSetStorageString failed', error);
      return 0;
    }
  },
  WxGetStorageString: function (keyPtr) {
    var key = UTF8ToString(keyPtr);
    var value = '';
    try {
      if (typeof wx !== 'undefined' && wx.getStorageSync) {
        value = wx.getStorageSync(key) || '';
      } else {
        value = localStorage.getItem(key) || '';
      }
    } catch (error) {
      console.warn('WxGetStorageString failed', error);
    }

    return stringToNewUTF8(value);
  },
  WxDeleteStorageKey: function (keyPtr) {
    var key = UTF8ToString(keyPtr);
    try {
      if (typeof wx !== 'undefined' && wx.removeStorageSync) {
        wx.removeStorageSync(key);
      } else {
        localStorage.removeItem(key);
      }
      return 1;
    } catch (error) {
      console.warn('WxDeleteStorageKey failed', error);
      return 0;
    }
  },
  WxGetStorageStatusString: function () {
    var status = '';
    try {
      if (typeof wx !== 'undefined' && wx.getStorageInfoSync) {
        status = JSON.stringify(wx.getStorageInfoSync());
      } else {
        status = JSON.stringify({ keys: Object.keys(localStorage), currentSize: 0, limitSize: 0 });
      }
    } catch (error) {
      console.warn('WxGetStorageStatusString failed', error);
      status = JSON.stringify({ error: String(error) });
    }

    return stringToNewUTF8(status);
  }
});
