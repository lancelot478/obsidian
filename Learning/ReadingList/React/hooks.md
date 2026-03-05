# useState 
* lazy initialization
* 
`const [state, setState] = useState(() => {

const initialState = someExpensiveComputation(props);

return initialState;
});`
* Functional updates
`const [state, setState] = useState({});

setState(prevState => {

return {...prevState, ...updatedValues};
});`

##  render优化
多次setState可能会放在同一个re-render update里，输入事件（多次点击）除外
# useEffect
* Mutations, subscriptions, timers, logging, and other side effects
* 在每次render complete之后调用，传入的方法会调用，但是processes 不会立即执行
* 清理资源 useEffect(() => {

  const subscription = props.source.subscribe();

  return () => { // Clean up the subscription }; });
* 依赖value更新
  useEffect({}，[value])
* 只在componentDidMount and componentWillUnmount调用 useEffect({}，[])
# useLayoutEffect
与useEffect相似，里面的useEffect和processes 立即执行