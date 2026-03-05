#Go

```go
package bitutil  
  
// Bitmap xxxtype Bitmap []uint16  
  
const maxCapacity = 0xffff  
  
// getSize get the length of Bitmap(the first element is the element's size)  
func getSize(n int) int {  
    return 1 + (n+15)>>4  
}  
  
// NewBitmap create a Bitmap, which capacity <= 65535  
func NewBitmap(capacity int) Bitmap {  
    if capacity > maxCapacity {  
       panic("capacity > 65535")  
    }    return newBitmapSize(getSize(capacity))  
}  
  
func newBitmapSize(sz int) Bitmap {  
    return make([]uint16, sz)  
}  
  
// Size get the element's sizefunc (b Bitmap) Size() int {  
    if len(b) <= 1 {  
       return 0  
    }  
    return int(b[0])  
}  
  
// Capacity get the element's capacityfunc (b Bitmap) Capacity() int {  
    l := len(b)  
    if l <= 1 {  
       return 0  
    }  
    return (l - 1) << 4  
}  
  
// UpdateCapacity update the bitmap's capacityfunc (b Bitmap) UpdateCapacity(n int) (Bitmap, bool) {  
    if n > maxCapacity {  
       return nil, false  
    }  
    newSize := getSize(n)  
    if newSize <= len(b) {  
       return nil, false  
    }  
    val := newBitmapSize(newSize)  
    for i, v := range b {  
       val[i] = v    }    return val, true  
}  
  
// Get get a free idfunc (b Bitmap) Get() uint32 {  
    size := uint32(len(b))  
    for i := uint32(1); i < size; i++ {  
       if b[i] == 0xffff {  
          continue  
       }  
       for j := uint32(0); j < 16; j++ {  
          if b[i]&(1<<j) == 0 {  
             b[i] |= 1 << j  
             b[0]++  
             // ID从1开始  
             return 1 + ((i-1)<<4 + j)  
          }       }    }    return 0  
}  
  
// Put put a idfunc (b Bitmap) Put(id uint32) bool {  
    // 注意ID从1开始  
    if id == 0 {  
       return false  
    }  
    idx := 1 + int(id-1)>>4  
    if idx >= len(b) {  
       return false  
    }  
    mask := uint16(1 << ((id - 1) & 0x000F))  
    if b[idx]&mask > 0 {  
       b[idx] &^= mask  
       b[0]--  
       return true  
    }  
    return false  
}  
  
// Set set a idfunc (b Bitmap) Set(id uint32) bool {  
    // 注意ID从1开始  
    if id == 0 {  
       return false  
    }  
    idx := 1 + int(id-1)>>4  
    if idx >= len(b) {  
       return false  
    }  
    mask := uint16(1 << ((id - 1) & 0x000F))  
    if b[idx]&mask == 0 {  
       b[idx] |= mask  
       b[0]++  
       return true  
    }  
    return false  
}
```

```go
排行榜更新排序，只需要从插入的位置向前比较排序
// 向前查找,比较并排序  
for i := rankIndex; i > 0; i-- {  
    next := i - 1  
    if r.rankList[i].Compare(r.rankList[next]) > 0 {  
       // 需要交换排行  
       r.rankList[i], r.rankList[next] = r.rankList[next], r.rankList[i]  
       // 更新玩家对应索引  
       r.rankIndex[r.rankList[i].RoleID] = i  
       r.rankIndex[r.rankList[next].RoleID] = next  
    } else {  
       // 已经是最大排行了  
       break  
    }  
}
```