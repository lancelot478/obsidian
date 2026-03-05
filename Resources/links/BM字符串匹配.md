#Algorithm

```Csharp
//a,主串 n,主串长度  b,模式串  m,模式串长度
private void generateBC(char[] b, int m, int[] bc)  
{  
    for (int i = 0; i < 256; i++)  
    {  
        bc[i] = -1;  
    }  
  
    for (int i = 0; i < m; i++)  
    {  
        var ascii = (int)b[i];  
        bc[ascii] = i;  
    }  
}  
  
private void generateGS(char[] b,int m,int[] suffix,bool[] prefix)  
{  
    for (int i = 0; i < m; i++)  
    {  
        suffix[i] = -1;  
        prefix[i] = false;  
    }  
  
    for (int i = 0; i < m-1; i++)  
    {  
        int j = i;  
        int k = 0;  
        while (j >= 0 && b[j] == b[m - 1 - k])  
        {  
            --j;  
            ++k;  
            suffix[k] = j + 1;  
        }  
        if (j == -1)  
        {  
            prefix[k] = true;  
        }  
    }}  
  
private int moveByGS(int j, int m,int[] suffix,bool[] prefix)  
{  
    int k = m - j - 1;  
    if (suffix[k] != -1)  
        return j - suffix[k] + 1;  
    for (int r = j + 2; r <= m - 1; ++r)  
    {  
        if (prefix[m - r])  
        {  
            return r;  
        }  
    }  
    return m;  
}  
public int bm(char[] a,int n,char[] b,int m){  
    int[] bc = new int[256];  
    generateBC(b, m, bc);  
    int[] suffix = new int[m];  
    bool[] prefix = new bool[m];  
    generateGS(b,m,suffix,prefix);  
    int i = 0;  
    while (i <= n - m)  
    {  
        int j;  
        for (j = m - 1; j < 0; --j)  
        {  
            if (b[j] != a[i + j])  
            {  
                break;  
            }  
        }        
        if (j < 0)  
        {  
            return i;  
        }  
        //坏字符原则  
        int x = j - bc[(int)a[i + j]];  
        //好后缀原则  
        int y = 0;  
        if (j < m - 1)  
        {  
            y = moveByGS(j, m, suffix, prefix);  
        }  
  
        i += Math.Max(x, y);  
    }  
    return -1;  
}
```