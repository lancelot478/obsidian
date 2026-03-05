

 pointerId: 键的id, 使用鼠标的情况下, -1代表左键, -2代表右键, -3代表中键, 使用触摸时未测试, 后面有机会测试后补充上.
 bool eligibleForClick: 此时事件是否是合格的点击, 后面可以看到, 如果在处理点击事件之前处理了移动事件则会取消此状态导致点击事件无法处理, 这个状态是ScrollRect能够同时滑动和点击的关键.
 Vector2 position: 当前触摸或者点击的位置, 屏幕坐标为单位
 Vector2 delta: 上次更新到此时更新间的位置变化量
 Vector2 pressPosition: 当前(或者最后一次)点击或者触摸的位置
 float clickTime: 上次点击或者触摸的时间
clickCount: 点击次数, 用于短时间内快速多次点击, 比如双击等
 Vector2 scrollDelta: 上次更新到此时更新间的滚动变化量
 bool useDragThreshold: 是否使用拖拽阈值, 就是之前在上一篇文章介绍的EventSystem组件上设置的值
 bool dragging: 当前是否是拖拽状态
 InputButton button: 当前事件的按键
 bool IsPointerMoving: 上次更新到此时更新间是否发生了移动
 bool IsScrolling: 上次更新到此时更新间是否发生了滚动


public GameObject pointerEnter: 鼠标进入的对象, 也就是说鼠标指针的位置在某个对象区域内部
public GameObject lastPress: 上一个接收到OnPointerDown事件的对象
public GameObject rawPointerPress: 原始按下的对象, 不论是否处理OnPointerDown事件, 这里可能会有点疑问, 等后面我们介绍事件处理的时候就明白了
public GameObject pointerPress: 当前已接收到OnPointerDown事件的对象
public GameObject pointerDrag: 当前已接收到OnDrag事件的对象
public RaycastResult pointerCurrentRaycast: 与当前事件关联的 RaycastResult
public RaycastResult pointerPressRaycast: 与最后一按下事件关联的RaycastResult
public List<GameObject> hovered: 鼠标悬停时, pointerEnter和其所有父节点和祖先节点形成的对象列表
public Camera enterEventCamera: 与当前事件相关联的摄像机
public Camera pressEventCamera:与最后一按下事件关联的RaycastResult




