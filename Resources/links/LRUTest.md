#Algorithm
```java
// 使用双向链表 和 哈希表 来实现  
class ALNode{  
  
    // 节点值  
    public int val;  
    // 当前节点的下一个节点  
    public ALNode nextNode;  
    // 当前节点的上一个节点  
    public ALNode preNode;  
    // 构造  
    ALNode(int val){  
        this.val = val;  
    }  
}  
  
public class LRUCache {  
  
    // 利用哈希表来存储元素  
    public Map<Integer, Object[]> maps;  
    // 为了方便使用，默认双向链表有两个节点  
    // 这样，哪怕只有一个节点时，依旧有上节点、下节点  
    // 头节点  
    public ALNode head;  
    // 头节点  
    public ALNode tail;  
    // 实际容量，意味着最多存储这么多节点  
    private int capacity;  
    // 长度  
    public int length;  
  
    public LRUCache(int capacity) {  
        // 初始化 head  
        head = new ALNode(-1);  
        // 初始化 tail  
        tail = new ALNode(-1);  
        // 初始化 maps  
        maps = new HashMap<>();  
        // 初始化 capacity  
        this.capacity = capacity;  
        // 连接 head 和 tail  
        head.nextNode = tail;  
        tail.preNode = head;  
    }  
  
    public int get(int key) {  
  
        // 判断哈希表中是否存储了 key  
        // 如果存在，不仅需要返回 key 的 value  
        // 同样，需要操作双向链表，使得  
        // 1、当前这个 key 对应的节点放到链表的最前面，即 head 的下一个节点  
        // 2、其余节点维持原来的顺序  
        if(maps.containsKey(key)){  
  
            // 获取节点值  
            ALNode cur = (ALNode) maps.get(key)[0];  
  
            // 获取当前节点的上一个节点  
            ALNode preNode = cur.preNode;  
  
            // 获取当前节点的下一个节点  
            ALNode nextNode = cur.nextNode;  
  
            // 让这两个上下节点连接起来，cur 也就消失了  
            preNode.nextNode = nextNode;  
  
            nextNode.preNode = preNode;  
  
            // 把 cur 挪到 head 的 nextNode 位置  
            // 1、先获取原先 head 的 nextNode 节点  
            ALNode tmp = head.nextNode;  
  
            // 2、修改 head 的 nextNode 节点为 cur  
            head.nextNode = cur;  
  
            // 3、cur 重新连接上 tmp  
            cur.nextNode = tmp;  
  
            // 4、tmp 也连接上 cur  
            tmp.preNode = cur;  
  
            // 5、cur 上一个节点指向 head  
            cur.preNode = head;  
  
            // 最后才返回 map 的值  
            return (Integer) maps.get(key)[1];  
        }  
  
        // 否则返回 -1   
        return -1;  
    }  
  
    public void put(int key, int value) {  
          
        // 判断哈希表中是否存储了 key  
        // 如果存在，不仅需要返回 key 的 value  
        // 同样，需要操作双向链表，使得  
        // 1、key 对应的节点值 value 需要修改，采取节点替换的操作  
        // 2、这个节点需要挪到最前面  
        if(maps.containsKey(key)){  
  
            // 获取节点值  
            ALNode cur = (ALNode) maps.get(key)[0];  
  
            // 获取当前节点的上一个节点  
            ALNode preNode = cur.preNode;  
  
            // 获取当前节点的下一个节点  
            ALNode nextNode = cur.nextNode;  
  
            // 让这两个上下节点连接起来，cur 也就消失了  
            preNode.nextNode = nextNode;  
  
            nextNode.preNode = preNode;  
  
            // 把 cur 挪到 head 的 nextNode 位置  
            // 1、先获取原先 head 的 nextNode 节点  
            ALNode tmp = head.nextNode;  
  
            // 2、修改 head 的 nextNode 节点为 cur  
            head.nextNode = cur;  
  
            // 3、cur 重新连接上 tmp  
            cur.nextNode = tmp;  
  
            // 4、tmp 也连接上 cur  
            tmp.preNode = cur;  
  
            // 5、cur 上一个节点指向 head  
            cur.preNode = head;  
  
            // 更新节点  
            maps.put(key, new Object[]{cur, value});  
  
            return;  
        }  
  
        // 如果哈希表中不包含 key 对应的节点，那么需要判断缓存是否满了  
        // 如果满了，需要把最后一个节点删除掉  
        if(length == capacity){  
              
            // 即将被删除的节点  
            ALNode delNode = tail.preNode;  
  
            // 即将被删除的节点的上一个节点  
            ALNode delPreNode = tail.preNode.preNode;  
  
            // delPreNode 跳过了 delNode  
            delPreNode.nextNode = tail;  
  
            tail.preNode = delPreNode;  
              
            // 哈希表移除 delNode 对应的值  
            maps.remove(delNode.val);  
  
            // 链表的长度更新一下  
            length--;  
        }  
  
        // 再把 key 节点添加到最前面去  
        ALNode cur = new ALNode(key);  
  
        // 把 cur 挪到 head 的 nextNode 位置  
        // 1、先获取原先 head 的 nextNode 节点  
        ALNode tmp = head.nextNode;  
  
        // 2、修改 head 的 nextNode 节点为 cur  
        head.nextNode = cur;  
  
        // 3、cur 重新连接上 tmp  
        cur.nextNode = tmp;  
  
        // 4、tmp 也连接上 cur  
        tmp.preNode = cur;  
  
        // 5、cur 上一个节点指向 head  
        cur.preNode = head;  
  
        // 更新哈希表  
        maps.put(key, new Object[]{cur, value});  
  
        // 更新 length  
        length++;  
          
        return;  
    }  
}
```