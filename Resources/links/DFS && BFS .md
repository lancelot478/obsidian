#Algorithm

[一文秒杀所有岛屿题目 | labuladong 的算法笔记](https://labuladong.online/algo/frequency-interview/island-dfs-summary/)

### 着眼点永远是在单一的节点上，类比到二叉树上就是处理每个「节点」

```javascript
void dfs(int[][] grid,int i, int j){
	int m = grid.length,n = grid[0].length;
	if(i<0||j<0||i>=m||j>=n){
		return;
	}
	if(grid[i][j] == 0){
		return;
	}
	grid[i][j] = 0;
	dfs(grid,i,j-1);
}
// DFS 算法把「做选择」「撤销选择」的逻辑放在 for 循环外面
var dfs = function(root) {
    if (root == null) return;
    // 做选择
    console.log("我已经进入节点 "+ root +" 啦");
    for (var i in root.children) {
        dfs(root.children[i]);
    }
    // 撤销选择
    console.log("我将要离开节点 "+ root +" 啦");
}

// 回溯算法把「做选择」「撤销选择」的逻辑放在 for 循环里面
var backtrack = function(root) {
    if (root == null) return;
    for (var i in root.children) {
        // 做选择
        console.log("我站在节点 "+ root +" 到节点 "+ root.children[i] +" 的树枝上");
        backtrack(root.children[i]);
        // 撤销选择
        console.log("我将要离开节点 "+ root.children[i] +" 到节点 "+ root +" 的树枝上");
    }
}
```
### 层序遍历
```javascript
//传统方式
var levelTraverse = function(root) {
    if (root === null) return;
    var q = new Queue();
    q.push(root);

    // 从上到下遍历二叉树的每一层
    while (!q.isEmpty()) {
        var sz = q.size();
        // 从左到右遍历每一层的每个节点
        for (var i = 0; i < sz; i++) {
            var cur = q.pop();
            // 将下一层节点放入队列
            if (cur.left !== null) {
                q.push(cur.left);
            }
            if (cur.right !== null) {
                q.push(cur.right);
            }
        }
    }
}
//递归方式
var levelTraverse = function(root){
	let res = [];
	traverse(root,0);
	return res;
}
var traverse = function(root,depth){
	if(root == null){
		return;
	}
	if(res.length <= depth){
		res.push([]);
	}
	res[depth].push(root.val);
	
	traverse(root.left,depth+1);
	traverse(root.right,depth+1)
}
```