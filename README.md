# 口袋城市规划师微信小游戏版

这是一个非 Unity 的微信小游戏工程。当前上线路径使用 TypeScript 共享模拟核心，并为微信小游戏生成 Canvas 2D runtime。Unity 工程代码已从活跃仓库移除；后续功能只在 `browser/src` 与 `miniprogram/` 生成包链路内推进。

## 当前入口
- `browser/` 是当前活跃开发工程，使用 TypeScript + Vite 构建。
- `browser/src/simulation/` 是纯模拟核心，不依赖 DOM、Phaser 或微信 API。
- `browser/src/game/` 与 `browser/src/ui/` 只服务浏览器调试版。
- `browser/src/wechat/main.ts` 是微信小游戏 Canvas 2D 入口，不依赖 DOM、Phaser、Worker、WebGL2、SharedArrayBuffer 或 Unity。
- `miniprogram/game.js` 由 `npm run build:wechat` 生成，不要手改。

## 已有玩法核心
当前微信 Canvas runtime 已具备：

- 等距城市网格、确定性水域/丘陵地形和横屏 Canvas HUD。
- 查看、道路、住宅、商业、工业、公园、诊所、学校、清理等规划工具。
- 道路铺设、道路升级、道路容量、道路覆盖、拥堵与通勤提示。
- 住宅/商业/工业分区、需求驱动自然开发、建筑年龄、住宅升级、混合核心和办公成长。
- 功能缓冲、用地冲突、用地效率、发展品质、地块检查和图层诊断。
- 人口、现金、幸福度、评分、城市等级、解锁、告警和近期事件。
- 材料仓库、工厂生产队列、订单交付、目标奖励和离线生产结算。
- 公园/医疗/教育服务覆盖，游客经济、人才/劳动力素质、经济专精和服务短板洞察。
- 税率、暂停/倍速、九项城市政策、政策预览、行政容量与政策积压。
- 微信本地存档、`onHide` 自动保存、`onShow` 读档与离线推进、触觉反馈 fallback。

这些系统应优先被打磨、验证和补齐微信端体验；近期不新增大玩法线。

## 开发命令
```bash
npm --prefix browser run dev
npm --prefix browser run build
npm run build:wechat
npm run smoke:wechat
npm run verify
```

`npm run verify` 会构建当前非 Unity 微信小游戏入口，运行活跃 Canvas runtime 静态门禁与微信烟测，确保 `miniprogram/game.js` 不是 Unity 占位文件，且不含 DOM、Phaser、Worker、WebGL2、SharedArrayBuffer 等微信 runtime 禁用项。

## 微信预览
1. 运行 `npm run build:wechat`。
2. 用微信开发者工具打开 `miniprogram/`。
3. 使用横屏小游戏模式预览。
4. 记录包体大小、首帧表现、操作帧率、存档恢复和真机触控体验。

## 架构约束
- 不恢复 Unity 工程、Unity WebGL 转换、`.jslib` 桥或 Unity 生成资产链路。
- 不把 Phaser、DOM、Worker、WebGL2、SharedArrayBuffer 引入 `browser/src/wechat/main.ts` 或 `miniprogram/game.js`。
- 共享模拟逻辑放在 `browser/src/simulation/` 和 `browser/src/types/`。
- 微信平台能力只通过 `browser/src/wechat/main.ts` 的 `WeChatRuntime` 接口使用。
- 不复制现有城市建设 IP 的素材、命名、任务文本或平衡数值。
