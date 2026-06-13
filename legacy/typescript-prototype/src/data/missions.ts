export const STARTER_MISSIONS = [
  {
    id: 'first-road',
    title: '铺设主干道路',
    hint: '道路是城市服务和通勤的骨架。',
    target: 'roadTiles',
    required: 10,
  },
  {
    id: 'first-neighborhood',
    title: '接入第一组建筑',
    hint: '让建筑靠近道路，才能发挥完整作用。',
    target: 'connectedBuildings',
    required: 6,
  },
  {
    id: 'welcome-neighbors',
    title: '吸引第一批居民',
    hint: '住宅、岗位和基础服务会推动人口增长。',
    target: 'population',
    required: 120,
  },
  {
    id: 'first-park-service',
    title: '补足生活服务',
    hint: '建造口袋公园，让住宅覆盖在公共服务半径内。',
    target: 'serviceCoverage',
    required: 35,
  },
  {
    id: 'stable-town',
    title: '打造稳定街区',
    hint: '平衡服务、拥堵、污染和财政来提高评分。',
    target: 'cityScore',
    required: 72,
  },
];
