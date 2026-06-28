# QA 清单

## 自动门禁
运行：

```bash
npm run verify
```

必须确认：

- `browser` TypeScript 编译通过。
- `miniprogram/game.js` 重新生成。
- `miniprogram/game.js` 包含 `NON_UNITY_WECHAT_CANVAS_RUNTIME`。
- `miniprogram/game.json` 不含 `workers`。
- 微信 runtime 不含 DOM、Phaser、Worker、WebGL2、SharedArrayBuffer、createImageBitmap 或 Unity 占位/桥接标记。
- 烟测能创建 Canvas、注册触摸、注册 `onHide`/`onShow`、绘制首帧、选择道路工具、在地图放置道路、切换税率、切换政策、保存和读档。

## 浏览器调试
运行：

```bash
npm --prefix browser run dev
```

检查：

- 地图可见且能平移/缩放。
- HUD 顶部指标、侧栏、管理面板和底部工具栏不重叠。
- 规划工具可以铺路、划住宅/商业/工业、建设服务建筑和清理地块。
- 生产、订单、住宅升级、道路升级、税率、时间倍率和政策按钮有明确反馈。
- 需求、告警、近期事件、目标建议和地块检查文本可读。

## 微信开发者工具
运行：

```bash
npm run build:wechat
```

用微信开发者工具打开 `miniprogram/`，检查：

- 横屏小游戏模式可启动。
- 首帧不是空白画面。
- 地图、顶部状态栏、左右面板、底部工具栏和状态提示在目标机型宽高下不裁切。
- 点击工具栏后触觉反馈可用；不可用时游戏继续运行。
- 道路和分区点击能立即反映到地图和存档。
- 管理面板税率、倍速、政策按钮能切换并保存。
- 切后台触发保存，回到前台能读档并结算离线进度。
- 真机触控下单指平移、双指缩放和点击放置不会互相误触。

## 发布候选记录
发布前记录：

- `miniprogram/game.js` 大小和 gzip 大小。
- 微信开发者工具基础库版本。
- 目标真机型号。
- 首帧时间。
- 30 秒平均 FPS。
- 连续操作 2 分钟是否出现卡死、黑屏、控制台错误或存档失败。

## 回归重点
每次触碰 `browser/src/wechat/main.ts`、`browser/src/simulation/`、`tools/verify-wechat-runtime.mjs` 或 `tools/smoke-wechat-runtime.mjs` 后，至少运行：

```bash
npm run smoke:wechat
npm run verify
```
