# 技术方案

## 架构定位
项目当前是非 Unity 微信小游戏。活跃运行时代码位于 `browser/src`，微信包由 Vite 构建到 `miniprogram/game.js`。

```text
browser/src/simulation
  -> city-simulation.ts / grid.ts
  -> browser/src/types
  -> browser debug runtime: browser/src/game + browser/src/ui
  -> WeChat runtime: browser/src/wechat/main.ts
  -> generated package: miniprogram/game.js
```

## 分层约束
- `browser/src/simulation/`：纯城市模拟、指标、存档和目标逻辑，不依赖 DOM、Phaser 或微信 API。
- `browser/src/types/`：共享枚举、指标、订单、政策、检查器和解锁类型。
- `browser/src/game/`：浏览器调试版场景，可使用 Phaser。
- `browser/src/ui/`：浏览器调试版 DOM HUD。
- `browser/src/wechat/main.ts`：微信 Canvas 2D runtime，直接绘制 HUD、地图和按钮，并通过 `WeChatRuntime` 接口访问微信能力。
- `miniprogram/game.js`：构建产物，只由 `npm run build:wechat` 生成。

## 微信 Runtime 禁用项
`browser/src/wechat/main.ts` 和生成后的 `miniprogram/game.js` 不允许包含：

- DOM 依赖，例如 `document`、`window.`
- Phaser
- Worker
- WebGL2
- `SharedArrayBuffer`
- `createImageBitmap`
- Unity 占位符、UnityEngine、Unity WebGL 桥接代码

`tools/verify-wechat-runtime.mjs` 会静态检查这些约束，`tools/smoke-wechat-runtime.mjs` 会在 mock 微信环境里执行生成包。

## 模拟核心
当前模拟核心包含：

- 24x18 等距城市网格、地形、分区、道路、建筑和服务建筑。
- 人口、现金、幸福度、城市评分、等级经验和解锁。
- 住宅/商业/工业需求、需求驱动、风险、预算、成长瓶颈、片区优先级和告警摘要。
- 道路覆盖、拥堵、通勤、道路升级和道路层级建议。
- 公共服务覆盖、公园/医疗/教育、服务短板、游客、人才和经济专精。
- 材料仓库、生产队列、订单、目标奖励、离线生产结算和存档恢复。
- 税率、时间倍率、九项政策、政策预览、行政容量和政策积压。

## 存档
存档由 `CitySimulation.createSnapshot()` 生成，微信 runtime 通过 `wx.setStorageSync` 保存。恢复时通过 `wx.getStorageSync` 读取并调用 `restoreSnapshot()`，版本 3 存档会结算离线生产。

微信生命周期：

- `onHide`：保存当前城市。
- `onShow`：读取存档，恢复城市，并结算离线推进。
- storage 或触觉 API 不可用时，runtime 应显示状态反馈并继续当前城市。

## 输入与界面
微信 Canvas runtime 自绘：

- 顶部状态栏。
- 左侧地块/城市侧栏。
- 右侧管理面板。
- 底部工具栏。
- 状态提示条。

交互路径：

- 底部工具按钮切换规划工具。
- 地图点击应用工具或检查地块。
- 查看模式支持单指平移。
- 双指触控支持缩放。
- 管理面板支持生产、订单、升级、税率、时间倍率和政策按钮。

## 验证
必须通过：

```bash
npm run verify
```

该命令会：

1. 构建微信包。
2. 运行非 Unity 微信 runtime 静态门禁。
3. 在 mock 微信环境中执行生成后的 `miniprogram/game.js`。
4. 验证首帧绘制、触摸注册、生命周期注册、道路工具落子、管理面板税率/政策按钮、保存和读档。

发布候选还需要在微信开发者工具和真机上记录包体大小、首帧、平均帧率、触摸延迟和存档恢复。
