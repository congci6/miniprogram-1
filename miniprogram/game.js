// UNITY_BUILD_PENDING
// This mini game root is now reserved for Unity / Tuanjie WebGL conversion output.
// Replace this file by exporting the Unity project in ../unity and converting it
// with the WeChat mini game SDK.

const message = 'Pocket City Planner has switched to the Unity architecture. Export the Unity project to generate the playable mini game.';

if (typeof wx !== 'undefined' && wx.showModal) {
  wx.showModal({
    title: 'Unity build pending',
    content: message,
    showCancel: false,
  });
} else {
  console.log(message);
}
