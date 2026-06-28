# Codex 任务记录

## 当前方向
继续推进非 Unity 微信小游戏城市规划玩法。活跃代码在 `browser/src`，微信包由 `npm run build:wechat` 生成到 `miniprogram/game.js`。近期优先完善已有功能、验证、UI 适配和上线稳定性，不开新的大型玩法系统。

## 已完成
- 非 Unity 微信 Canvas runtime 已可启动并绘制首帧。
- 默认 `npm run verify` 已切到当前微信 runtime 门禁。
- Unity 工程代码、Unity WebGL 桥、Unity scaffold 校验脚本和 Unity 专项文档已从活跃仓库移除。
- 微信 runtime 静态门禁覆盖 DOM、Phaser、Worker、WebGL2、SharedArrayBuffer、createImageBitmap、Unity 占位符和 Unity 桥接标记。
- 微信烟测覆盖 Canvas 创建、触摸注册、生命周期注册、首帧绘制、道路工具落子、管理面板税率/政策按钮、保存和读档。
- 浏览器调试版仍保留 Phaser 入口，便于本地调试共享模拟。
- 微信端 HUD 已包含顶部状态栏、侧栏、管理面板、底部工具栏和状态提示。
- 当前模拟已包含道路、分区、自然开发、服务覆盖、生产订单、目标、政策、税率、离线推进和本地存档。

## 近期优先级
1. 微信端 UI 拥挤回归检查：小屏横屏、面板文本截断、按钮点击区域、状态提示不重叠。
2. 微信端交互烟测继续补强：工具栏更多按钮、查看模式平移、双指缩放、生产/订单/升级按钮。
3. 存档兼容与异常路径：空存档、坏存档、storage 不可用、触觉 API 不可用。
4. 模拟稳定性：自然开发、订单、政策、税率、目标奖励和离线推进不要互相破坏。
5. 微信开发者工具与真机记录：包体、首帧、FPS、触控延迟和存档恢复。

## 不做
- 不恢复 Unity 工程。
- 不恢复 Unity WebGL 转换链路或 `.jslib` 桥。
- 不把 DOM、Phaser、Worker、WebGL2 或 SharedArrayBuffer 引入微信 runtime。
- 不手改 `miniprogram/game.js`。
- 不复制现有城市建设 IP 的素材、命名、任务文本或平衡数值。

## 下一步建议
- 为 `tools/smoke-wechat-runtime.mjs` 增加生产/订单/升级按钮覆盖。
- 为微信 runtime 增加坏存档恢复烟测。
- 用微信开发者工具做一次横屏预览记录，并把包体和 FPS 写入 QA 记录。
