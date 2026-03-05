

# 什么是环形队列？

环形缓冲区是一个非常典型的数据结构，这种数据结构符合生产者，消费者模型，可以理解它是一个水坑，生产者不断的往里面灌水，消费者就不断的从里面取出水。

![图片](https://mmbiz.qpic.cn/mmbiz_png/Qof5hj3zMPey1sZS2z2DeTnX7KSJicQmicP2iczLGUzfRLPxCU1APBcbM5vzybM9xicRrCmHtv9lg1z3XSpKsFleGg/640?wx_fmt=png&wxfrom=5&wx_lazy=1&wx_co=1)

那就可能会有人问，既然需要灌水，又需要取出水，为什么还需要开辟一个缓冲区内存空间呢？**直接把生产者水管的尾部接到消费者水管的头部不就好了，这样可以省空间啊。**

![图片](https://mmbiz.qpic.cn/mmbiz_png/Qof5hj3zMPey1sZS2z2DeTnX7KSJicQmicCYEtksoOYaxic1iagqSJJ0kZB8MRhNWs8xZTPrrSOSfrkJ6gQHibNcpdg/640?wx_fmt=png&wxfrom=5&wx_lazy=1&wx_co=1)

**答案是不行的，**生产者生产水的速度是不知道的，消费者消费水的速度也是不知道的，如果你强制接在一起，因为生产和消费的速度不同，就非常可能存在水管爆炸的情况，你说这样危险不危险？

![图片](https://mmbiz.qpic.cn/mmbiz_png/Qof5hj3zMPey1sZS2z2DeTnX7KSJicQmickCAcKmS1zX14gqeoSF7btrpicLWq7FDd2eTictxJfu7HOmiasNJwbxY7w/640?wx_fmt=png&wxfrom=5&wx_lazy=1&wx_co=1)

  

# 在音频系统框架下，alsa就是使用环形队列的，在生产者和消费者速度不匹配的时候，就会出现xrun的问题。

# 环形队列的特点

## 1、数组构造环形缓冲区

假设我们用数组来构造一个环形缓存区，如下图



我们需要几个东西来形容这个环形缓冲区，一个的读位置，一个是写位置，一个是环形缓冲区的长度  

从图片看，我们知道，这个环形缓冲区的读写位置是指向数组的首地址的，环形缓冲区的长度是 5 。  

那如何判断环形缓冲区为空呢？

如果 R == W  就是读写位置相同，则这个环形缓冲区为空

那如何判断环形缓冲区满了呢？

如果 （W - R ）= Len ，则这个环形缓冲区已经满了。

## 2、向环形缓冲区写入 3个数据

写入 3 个数据后，W 的值等于 3 了，R 还是等于 0。

3个企鹅已经排列

## 3、从环形缓冲区读取2个数据

读出两个数据后，R = 2 了，这个时候，W还是等于 3，毕竟没有再写过数据了。

## 4、再写入3个数据

如果 W > LEN 后，怎么找到最开始的位置的呢？这个就需要进行运算了，W%LEN 的位置就是放入数据的位置 ，6%5 = 1。

## 5、再写入1个数据

这个时候环形队列已经满了，要是想再写入数据的话，就不行了，**(W - R) = 5 == LEN**

# 代码实现

```c
/* 实现的最简单的ringbuff 有更多提升空间，可以留言说明 */
#include "stdio.h"
#include "stdlib.h"

#define LEN 10

/*环形队列结构体*/
typedef struct ring_buff{
	int array[LEN];
	int W;
	int R;
}*ring;

/*环形队列初始化*/
struct ring_buff * fifo_init(void)
{
	struct ring_buff * p = NULL;
	p = (struct ring_buff *)malloc(sizeof(struct ring_buff));
	if(p == NULL)
	{
	   printf("fifo_init malloc error\n");
	   return NULL;
	}
	p->W = 0;
	p->R = 0;
	return p;
}

/*判断环形队列是否已经满了*/
int get_ring_buff_fullstate(struct ring_buff * p_ring_buff)
{
	/*如果写位置减去读位置等于队列长度，就说明这个环形队列已经满*/
	if((p_ring_buff->W - p_ring_buff->R) == LEN)
	{
		return (1);
	}
	else
	{
		return (0);
	}
}

/*判断环形队列为空*/
int get_ring_buff_emptystate(struct ring_buff * p_ring_buff)
{
	/*如果写位置和读的位置相等，就说明这个环形队列为空*/
	if(p_ring_buff->W == p_ring_buff->R)
	{
		return (1);
	}
	else
	{
		return (0);
	}
}
/*插入数据*/
int ring_buff_insert(struct ring_buff * p_ring_buff,int data)
{
	if(p_ring_buff == NULL)
	{
	   printf("p null\n");
	   return (-1);	
	}
	
	if(get_ring_buff_fullstate(p_ring_buff) == 1)
	{
		printf("buff is full\n");
		return (-2);
	}
	
	p_ring_buff->array[p_ring_buff->W%LEN] = data;
	
	p_ring_buff->W ++;
	//printf("inset:%d %d\n",data,p_ring_buff->W);
	return (0);
}

/*读取环形队列数据*/
int ring_buff_get(struct ring_buff * p_ring_buff)
{
	int data = 0;
	
	if(p_ring_buff == NULL)
	{
	   printf("p null\n");
	   return (-1);	
	}
	
	if(get_ring_buff_emptystate(p_ring_buff) == 1)
	{
		printf("buff is empty\n");
		return (-2);
	}
	
	data = p_ring_buff->array[p_ring_buff->R%LEN];
	p_ring_buff->R++;
	return data;
}

/*销毁*/
int ring_buff_destory(struct ring_buff * p_ring_buff)
{
	if(p_ring_buff == NULL)
	{
	   printf("p null\n");
	   return (-1);	
	}
	
	free(p_ring_buff);
	
	return (0);
}

int main()
{
	int i = 0;
	
	/*定义一个环形缓冲区*/
	ring pt_ring_buff = fifo_init();
	
	/*向环形缓冲区中写入数据*/
	for(i = 0;i<10;i++)
	{
		ring_buff_insert(pt_ring_buff,i);
	}
	
	/*从环形缓冲区中读出数据*/
	for(i = 0;i<10;i++)
	{
		printf("%d ",ring_buff_get(pt_ring_buff));
	}
	
	/*销毁一个环形缓冲区*/
	ring_buff_destory(pt_ring_buff);
	
	return (1);
}
```

换一个写法，这个写法是各种大神级别的  

```c
/* 实现的最简单的ringbuff 有更多提升空间，可以留言说明 */
#include "stdio.h"
#include "stdlib.h"

#define LEN 64

/*环形队列结构体*/
typedef struct ring_buff{
	int array[LEN];
	int W;
	int R;
}*ring;

/*环形队列初始化*/
struct ring_buff * fifo_init(void)
{
	struct ring_buff * p = NULL;
	p = (struct ring_buff *)malloc(sizeof(struct ring_buff));
	if(p == NULL)
	{
	   printf("fifo_init malloc error\n");
	   return NULL;
	}
	p->W = 0;
	p->R = 0;
	return p;
}

/*判断环形队列是否已经满了*/
int get_ring_buff_fullstate(struct ring_buff * p_ring_buff)
{
	/*如果写位置减去读位置等于队列长度，就说明这个环形队列已经满*/
	if((p_ring_buff->W - p_ring_buff->R) == LEN)
	{
		return (1);
	}
	else
	{
		return (0);
	}
}

/*判断环形队列为空*/
int get_ring_buff_emptystate(struct ring_buff * p_ring_buff)
{
	/*如果写位置和读的位置相等，就说明这个环形队列为空*/
	if(p_ring_buff->W == p_ring_buff->R)
	{
		return (1);
	}
	else
	{
		return (0);
	}
}
/*插入数据*/
int ring_buff_insert(struct ring_buff * p_ring_buff,int data)
{
	if(p_ring_buff == NULL)
	{
	   printf("p null\n");
	   return (-1);	
	}
	
	if(get_ring_buff_fullstate(p_ring_buff) == 1)
	{
		printf("buff is full\n");
		return (-2);
	}
	
	//p_ring_buff->array[p_ring_buff->W%LEN] = data;
	p_ring_buff->array[p_ring_buff->W&(LEN -1)] = data;	
	p_ring_buff->W ++;
	//printf("inset:%d %d\n",data,p_ring_buff->W);
	return (0);
}

/*读取环形队列数据*/
int ring_buff_get(struct ring_buff * p_ring_buff)
{
	int data = 0;
	
	if(p_ring_buff == NULL)
	{
	   printf("p null\n");
	   return (-1);	
	}
	
	if(get_ring_buff_emptystate(p_ring_buff) == 1)
	{
		printf("buff is empty\n");
		return (-2);
	}
	
	//data = p_ring_buff->array[p_ring_buff->R%LEN];
	data = p_ring_buff->array[p_ring_buff->R&(LEN -1)];
	p_ring_buff->R++;
	return data;
}

/*销毁*/
int ring_buff_destory(struct ring_buff * p_ring_buff)
{
	if(p_ring_buff == NULL)
	{
	   printf("p null\n");
	   return (-1);	
	}
	
	free(p_ring_buff);
	
	return (0);
}

int main()
{
	int i = 0;
	
	/*定义一个环形缓冲区*/
	ring pt_ring_buff = fifo_init();
	
	/*向环形缓冲区中写入数据*/
	for(i = 0;i<10;i++)
	{
		ring_buff_insert(pt_ring_buff,i);
	}
	
	/*从环形缓冲区中读出数据*/
	for(i = 0;i<10;i++)
	{
		printf("%d ",ring_buff_get(pt_ring_buff));
	}
	
	/*销毁一个环形缓冲区*/
	ring_buff_destory(pt_ring_buff);
	
	return (1);
}
```

# 总结  

环形队列的使用场景非常多，安卓的音频数据读写，很多都用到环形队列，我们在开发过程中使用的环形队列肯定比我上面的那个例子要复杂的多，我这里演示的是比较简单的功能，**但是麻雀虽小，五脏俱全**，希望这个麻雀让你们了解这个数据结构。在实际项目中大展身手。

