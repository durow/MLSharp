﻿/*
 * Description:张量相关操作，实现了深度学习中用到的大多数计算，加减乘除和转置等。
 *             通过在一维数组的基础上加了一层中间件，用来映射张量的结构和相关操作。
 *             部分张量计算使用了Parallel进行了并行化。
 * Author:Yunxiao An
 * Date:2018.11.16
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MLStudy
{
    /// <summary>
    /// 标量
    /// </summary>
    public sealed class Tensor
    {
        private double[] values; //存放底层数据
        private int[] shape; //Tensor结构信息
        private int[] dimensionSize; //存储各个维度的大小

        /// <summary>
        /// Tensor的阶
        /// </summary>
        public int Rank { get { return shape.Length; } }

        /// <summary>
        /// Tensor的结构
        /// </summary>
        public int[] Shape
        {
            get
            {
                return GetShape();
            }
        }

        /// <summary>
        /// 获取Tensor指定位置的值
        /// </summary>
        /// <param name="index">指定的位置，需要符合Rank</param>
        /// <returns>指定位置对应的值</returns>
        public double this[params int[] index]
        {
            get
            {
                return GetValue(index);
            }
            set
            {
                SetValue(value, index);
            }
        }

        /// <summary>
        /// Tensor中所有元素的个数
        /// </summary>
        public long ElementCount
        {
            get
            {
                return values.Length;
            }
        }

        #region Creation

        /// <summary>
        /// 从data数组创建同样结构的Tensor
        /// </summary>
        /// <param name="data">数组，需要类型为double</param>
        public Tensor(Array data)
        {
            var shape = new int[data.Rank];
            for (int i = 0; i < shape.Length; i++)
            {
                shape[i] = data.GetLength(i);
            }
            InitTensor(shape);

            values = new double[data.LongLength];
            for (int i = 0; i < data.LongLength; i++)
            {
                values[i] = (double)data.GetValue(GetIndex(i));
            }
        }

        /// <summary>
        /// 创建一个由shape指定结构的Tensor，shape为空则创建Rank为0的标量
        /// </summary>
        /// <param name="shape">Tensor的结构</param>
        public Tensor(params int[] shape)
        {
            InitTensor(shape);
            values = new double[GetTotalLength(shape)];
        }

        /// <summary>
        /// 使用一维数组创建Tensor并转为shape指定的结构
        /// </summary>
        /// <param name="data">一维数组</param>
        /// <param name="shape">Tensor的结构，省略则按照一维Tensor(也就是Vector)处理</param>
        public Tensor(double[] data, params int[] shape)
        {
            if (shape.Length == 0)
                shape = new int[] { data.Length };

            var len = GetTotalLength(shape);
            if (len != data.Length)
                throw new TensorShapeException("data length and shape are not same!");

            InitTensor(shape);
            values = data;
        }

        /// <summary>
        /// 创建一个由shape指定结构的Tensor并用0-1的随机数填充
        /// </summary>
        /// <param name="shape">Tensor的结构</param>
        /// <returns>创建好的Tensor</returns>
        public static Tensor Rand(params int[] shape)
        {
            if (shape.Length == 0)
            {
                var result = new Tensor();
                result.SetValue(DataEmulator.Instance.Random());
                return result;
            }

            var len = GetTotalLength(shape);
            var data = DataEmulator.Instance.RandomArray(GetTotalLength(shape));
            return new Tensor(data, shape);
        }

        /// <summary>
        /// 创建一个由shape指定结构的Tensor并用符合N(0,1)高斯分布的数值填充
        /// </summary>
        /// <param name="shape">Tensor的结构</param>
        /// <returns>创建好的Tensor</returns>
        public static Tensor RandGaussian(params int[] shape)
        {
            if (shape.Length == 0)
            {
                var result = new Tensor();
                result.SetValue(DataEmulator.Instance.Random());
                return result;
            }

            var len = GetTotalLength(shape);
            var data = DataEmulator.Instance.RandomArrayGaussian(GetTotalLength(shape));
            return new Tensor(data, shape);
        }

        /// <summary>
        /// 创建一个由shape指定结构的Tensor并用0填充
        /// </summary>
        /// <param name="shape">Tensor的结构</param>
        /// <returns>创建好的Tensor</returns>
        public static Tensor Zeros(params int[] shape)
        {
            return new Tensor(shape);
        }

        /// <summary>
        /// 创建一个由shape指定结构的Tensor并用1填充
        /// </summary>
        /// <param name="shape">Tensor的结构</param>
        /// <returns>创建好的Tensor</returns>
        public static Tensor Ones(params int[] shape)
        {
            return Values(1, shape);
        }

        /// <summary>
        /// 创建一个由shape指定结构的Tensor并用参数value的值填充
        /// </summary>
        /// <param name="value">填充的值</param>
        /// <param name="shape">Tensor的结构</param>
        /// <returns>创建好的Tensor</returns>
        public static Tensor Values(double value, params int[] shape)
        {
            if (shape.Length == 0)
            {
                var result = new Tensor();
                result.SetValue(value);
                return result;
            }

            var len = GetTotalLength(shape);
            var data = new double[len];
            for (int i = 0; i < len; i++)
            {
                data[i] = value;
            }
            return new Tensor(data, shape);
        }

        /// <summary>
        /// 创建一个二维的单位矩阵
        /// </summary>
        /// <param name="width">矩阵的宽度和高度</param>
        /// <returns></returns>
        public static Tensor I(int width)
        {
            var result = new Tensor(width, width);
            for (int i = 0; i < width; i++)
            {
                result[i, i] = 1;
            }
            return result;
        }

        #endregion

        /// <summary>
        /// 获取Tensor指定位置的值，Rank为0，也就是Tensor为标量时指定index为空返回标量的值
        /// </summary>
        /// <param name="index">指定的位置</param>
        /// <returns>指定位置返回的值</returns>
        public double GetValue(params int[] index)
        {
            if (index.Length == 0)
                return values[0];

            if (index.Length != Rank)
                throw new TensorShapeException("index must be the same length as Rank!");

            for (int i = 0; i < Rank; i++)
            {
                if (index[i] >= shape[i])
                    throw new TensorShapeException($"index out of range! index is {index}, shape is {shape}");
            }

            if (index.Length == 0)
                return values[0];

            var offset = GetOffset(index);
            return values[offset];
        }

        /// <summary>
        /// 将Tensor中指定位置的值设置为value，Tensor为标量时保持index为空
        /// </summary>
        /// <param name="value">要设定的值</param>
        /// <param name="index">要设定的位置</param>
        public void SetValue(double value, params int[] index)
        {
            if (index.Length == 0 && ElementCount == 1)
            {
                values[0] = value;
                return;
            }

            if (index.Length != Rank)
                throw new TensorShapeException("index must be the same length as Rank!");

            for (int i = 0; i < Rank; i++)
            {
                if (index[i] >= shape[i])
                    throw new TensorShapeException($"index out of range! index is {index}, shape is {shape}");
            }

            if (index.Length == 0)
            {
                values[0] = value;
                return;
            }

            var offset = GetOffset(index);
            values[offset] = value;
        }

        /// <summary>
        /// 通过底层一维数组的index访问值，一般不建议使用
        /// </summary>
        /// <param name="index">底层一维数组的位置</param>
        /// <returns>index对应的值</returns>
        public double GetValueByRawIndex(int index)
        {
            return values[index];
        }

        /// <summary>
        /// 所有元素的和
        /// </summary>
        /// <returns>和</returns>
        public double Sum()
        {
            return values.Sum();
        }

        /// <summary>
        /// 所有元素的均值
        /// </summary>
        /// <returns>均值</returns>
        public double Mean()
        {
            return values.Average();
        }

        /// <summary>
        /// 所有元素中的最大值
        /// </summary>
        /// <returns>最大值</returns>
        public double Max()
        {
            return values.Max();
        }

        /// <summary>
        /// 所有元素中的最小值
        /// </summary>
        /// <returns>最小值</returns>
        public double Min()
        {
            return values.Min();
        }

        /// <summary>
        /// 转换Tensor的结构，新结构与原结构需要在元素数量上一致。
        /// 新的Tensor是原Tensor的新视图，与原Tensor共享同一个底层数据。
        /// </summary>
        /// <param name="shape">要转换的新的结构</param>
        /// <returns>转换后的Tensor</returns>
        public Tensor Reshape(params int[] shape)
        {
            if (shape.Length == 0)
                return new Tensor(values);

            var len = GetTotalLength(shape);
            if (len != ElementCount)
                throw new TensorShapeException($"can't reshape {this.shape.ToString()} to {shape.ToString()}");

            return new Tensor(values, shape);
        }

        /// <summary>
        /// 把当前Tensor转置，转置后返回的是一个新的Tensor
        /// </summary>
        /// <returns>转置后的Tensor</returns>
        public Tensor Transpose()
        {
            var result = new Tensor(shape.Reverse().ToArray());
            for (int i = 0; i < ElementCount; i++)
            {
                var index = GetIndex(i);
                var transIndex = index.Reverse().ToArray();
                result[transIndex] = values[i];
            }
            return result;
        }

        /// <summary>
        /// 创建一个新的Tensor并与现有Tensor结构相同
        /// </summary>
        /// <returns>新的Tensor</returns>
        public Tensor GetSameShape()
        {
            return new Tensor(shape);
        }

        /// <summary>
        /// 复制当前Tensor，包括结构和数据
        /// </summary>
        /// <returns></returns>
        public Tensor Clone()
        {
            var result = GetSameShape();
            values.CopyTo(result.values, 0);
            return result;
        }

        /// <summary>
        /// 以第一个维度为索引返回新的Tensor
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Tensor GetTensorByDim1(int index)
        {
            if (index >= shape[0])
                throw new TensorShapeException($"index out of range! index is {index} rank1 is {shape[0]}");

            if (Rank == 1)
            {
                var result = new Tensor();
                result.SetValue(this[index]);
                return result;
            }

            var len = dimensionSize[0];
            var start = index * len;
            var data = new double[len];
            var newShape = new int[Rank - 1];
            for (int i = 0; i < newShape.Length; i++)
            {
                newShape[i] = shape[i + 1];
            }

            for (int i = 0; i < len; i++)
            {
                data[i] = values[start + i];
            }

            return new Tensor(data, newShape);
        }

        /// <summary>
        /// 当前Tensor的所有元素应用function，结果保存在当前Tensor
        /// </summary>
        /// <param name="function">要应用的function</param>
        /// <returns>当前Tensor</returns>
        public Tensor Apply(Func<double, double> function)
        {
            Parallel.ForEach(Partitioner.Create(0, values.Length),
                arg =>
            {
                for (long i = arg.Item1; i < arg.Item2; i++)
                {
                    values[i] = function(values[i]);
                }
            });
            return this;
        }

        /// <summary>
        /// 指定Tensor的每个元素应用function，结果返回为新的Tensor
        /// </summary>
        /// <param name="tensor">应用function的Tensor</param>
        /// <param name="function">引用的function</param>
        /// <returns>结果返回为新的Tensor</returns>
        public static Tensor Apply(Tensor tensor, Func<double, double> function)
        {
            return tensor.Clone().Apply(function);
        }


        #region Add

        /// <summary>
        /// 把d加到当前Tensor的每个元素上
        /// </summary>
        /// <param name="d">要加的值</param>
        /// <returns>当前Tensor</returns>
        public Tensor Add(double d)
        {
            Apply(a => a + d);
            return this;
        }

        /// <summary>
        /// 把t加到当前Tensor，t和当前Tensor必须要有相同的结构
        /// </summary>
        /// <param name="t">要加的Tensor</param>
        /// <returns>当前Tensor</returns>
        public Tensor Add(Tensor t)
        {
            if (t.ElementCount == 1)
                return Add(t.GetValue());

            CheckShape(shape, t.shape);

            Parallel.ForEach(Partitioner.Create(0, values.Length),
                arg =>
                {
                    for (long i = arg.Item1; i < arg.Item2; i++)
                    {
                        values[i] += t.values[i];
                    }
                });
            return this;
        }

        /// <summary>
        /// 给t的每个元素加上d，结果返回为新的Tensor
        /// </summary>
        /// <param name="t">Tensor</param>
        /// <param name="d">要加上的值</param>
        /// <returns></returns>
        public static Tensor Add(Tensor t, double d)
        {
            return t.Clone().Add(d);
        }

        /// <summary>
        /// Tensor和Tensor对应元素相加，结果返回为新的Tensor，要求两个Tensor结构相同
        /// </summary>
        /// <param name="a">Tensor</param>
        /// <param name="b">Tensor</param>
        /// <returns>相加后的结果</returns>
        public static Tensor Add(Tensor a, Tensor b)
        {
            if (a.ElementCount == 1)
                return Add(b, a.GetValue());
            if (b.ElementCount == 1)
                return Add(a, b.GetValue());

            return a.Clone().Add(b);
        }

        public static Tensor operator +(Tensor t, double d)
        {
            return Add(t, d);
        }

        public static Tensor operator +(double d, Tensor t)
        {
            return Add(t, d);
        }

        public static Tensor operator +(Tensor a, Tensor b)
        {
            return Add(a, b);
        }

        #endregion


        #region Minus

        /// <summary>
        /// 当前Tensor的每个元素减去d，结果保存在当前Tensor中
        /// </summary>
        /// <param name="d">要减去的数值</param>
        /// <returns>当前Tensor</returns>
        public Tensor Minus(double d)
        {
            Apply(a => a - d);
            return this;
        }

        /// <summary>
        /// 用d减去当前Tensor的每个元素，结果保存在当前Tensor中
        /// </summary>
        /// <param name="d">被减数</param>
        /// <returns>当前Tensor</returns>
        public Tensor MinusBy(double d)
        {
            Apply(a => d - a);
            return this;
        }

        /// <summary>
        /// 当前Tensor和t中相应的元素相见，结果保存在当前Tensor中
        /// </summary>
        /// <param name="t">被减去的Tensor</param>
        /// <returns>当前Tensor</returns>
        public Tensor Minus(Tensor t)
        {
            if (t.ElementCount == 1)
                return Minus(t.GetValue());

            CheckShape(shape, t.shape);

            Parallel.ForEach(Partitioner.Create(0, values.Length),
                arg =>
                {
                    for (long i = arg.Item1; i < arg.Item2; i++)
                    {
                        values[i] -= t.values[i];
                    }
                });
            return this;
        }

        /// <summary>
        /// t的每个元素减去d，结果返回为新的Tensor
        /// </summary>
        /// <param name="t">被减数</param>
        /// <param name="d">减数</param>
        /// <returns>包含结果的新的Tensor</returns>
        public static Tensor Minus(Tensor t, double d)
        {
            return t.Clone().Minus(d);
        }

        /// <summary>
        /// 用d减去t的每个元素减去d，结果返回为新的Tensor
        /// </summary>
        /// <param name="d">被减数</param>
        /// <param name="t">减数</param>
        /// <returns>包含结果的新的Tensor</returns>
        public static Tensor Minus(double d, Tensor t)
        {
            return t.Clone().MinusBy(d);
        }

        /// <summary>
        /// 两个Tensor对应元素相减，结果返回为新的Tensor，要求两个Tensor结构相同
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Tensor Minus(Tensor a, Tensor b)
        {
            if (a.ElementCount == 1)
                return Minus(a.GetValue(), b);
            if (b.ElementCount == 1)
                return Minus(a, b.GetValue());

            return a.Clone().Minus(b);
        }

        public static Tensor operator -(Tensor t, double d)
        {
            return Minus(t, d);
        }

        public static Tensor operator -(double d, Tensor t)
        {
            return Minus(d, t);
        }

        public static Tensor operator -(Tensor a, Tensor b)
        {
            return Minus(a, b);
        }

        #endregion


        #region Multiple

        /// <summary>
        /// 当前Tensor乘上d，结果保存在当前Tensor
        /// </summary>
        /// <param name="d">要乘上的值</param>
        /// <returns>当前Tensor</returns>
        public Tensor Multiple(double d)
        {
            Apply(a => a * d);
            return this;
        }

        /// <summary>
        /// 当前Tensor和t的点积，结果保存在当前Tensor，要求两个Tensor结构一致
        /// </summary>
        /// <param name="t">乘上的Tensor</param>
        /// <returns>当前Tensor</returns>
        public Tensor MultipleElementWise(Tensor t)
        {
            if (t.ElementCount == 1)
                return Multiple(t.GetValue());

            CheckShape(shape, t.shape);

            Parallel.ForEach(Partitioner.Create(0, values.Length),
                arg =>
                {
                    for (long i = arg.Item1; i < arg.Item2; i++)
                    {
                        values[i] *= t.values[i];
                    }
                });
            return this;
        }

        /// <summary>
        /// Tensor每个元素乘上数字d，结果返回为新的Tensor
        /// </summary>
        /// <param name="t">Tensor</param>
        /// <param name="d">数字d</param>
        /// <returns>包含结果的新的Tensor</returns>
        public static Tensor Multiple(Tensor t, double d)
        {
            return t.Clone().Multiple(d);
        }

        /// <summary>
        /// 两个Tensor相乘，结果返回为新的Tensor。
        /// 目前仅支持scalar、vector、matrix的乘法
        /// </summary>
        /// <param name="a">Tensor1</param>
        /// <param name="b">Tensor2</param>
        /// <returns>包含结果的新的Tensor</returns>
        public static Tensor Multiple(Tensor a, Tensor b)
        {
            if (a.ElementCount == 1)
                return Multiple(b, a.GetValue());
            if (b.ElementCount == 1)
                return Multiple(a, b.GetValue());

            if (a.Rank == 1)
                a = a.Reshape(1, (int)a.ElementCount);
            if (b.Rank == 1)
                b = b.Reshape((int)b.ElementCount, 1);

            CheckMultipleShape(a, b);
            var result = new Tensor(a.shape[0], b.shape[1]);

            Parallel.For(0, a.shape[0], i =>
            {
                Parallel.For(0, b.shape[1], j =>
                {
                    var sum = 0d;
                    for (int k = 0; k < a.shape[1]; k++)
                    {
                        sum += a[i, k] * b[k, j];
                    }
                    result[i, j] = sum;
                });
            });

            return result;
        }

        /// <summary>
        /// 两个Tensor的点积，结果返回为新的Tensor，要求两个Tensor结构一致
        /// </summary>
        /// <param name="a">Tensor1</param>
        /// <param name="b">Tensor2</param>
        /// <returns>包含结果的新的Tensor</returns>
        public static Tensor MultipleElementWise(Tensor a, Tensor b)
        {
            if (a.ElementCount == 1)
                return Multiple(b, a.GetValue());
            if (b.ElementCount == 1)
                return Multiple(a, b.GetValue());

            return a.Clone().MultipleElementWise(b);
        }

        public static Tensor operator *(Tensor t, double d)
        {
            return Multiple(t, d);
        }

        public static Tensor operator *(double d, Tensor t)
        {
            return Multiple(t, d);
        }

        public static Tensor operator *(Tensor a, Tensor b)
        {
            return Multiple(a, b);
        }

        #endregion


        #region Divide

        /// <summary>
        /// 当前Tensor的每个元素除以d，结果保存在当前Tensor
        /// </summary>
        /// <param name="d">除数</param>
        /// <returns>当前Tensor</returns>
        public Tensor Divide(double d)
        {
            Apply(a => a / d);
            return this;
        }

        /// <summary>
        /// d除以当前Tensor的每个元素，结果保存在当前Tensor
        /// </summary>
        /// <param name="d">被除数</param>
        /// <returns>当前Tensor</returns>
        public Tensor DivideBy(double d)
        {
            Apply(a => d / a);
            return this;
        }

        /// <summary>
        /// 两个Tensor对应元素相除，结果保存在当前Tensor
        /// 还不知道有什么用 ;)
        /// </summary>
        /// <param name="t">除数</param>
        /// <returns>当前Tensor</returns>
        public Tensor DivideElementWise(Tensor t)
        {
            if (t.ElementCount == 1)
                return Divide(t.GetValue());

            CheckShape(shape, t.shape);

            Parallel.ForEach(Partitioner.Create(0, values.Length),
                arg =>
                {
                    for (long i = arg.Item1; i < arg.Item2; i++)
                    {
                        values[i] /= t.values[i];
                    }
                });
            return this;
        }

        /// <summary>
        /// Tensor的每个元素除以d，结果返回为新的Tensor
        /// </summary>
        /// <param name="t">被除数</param>
        /// <param name="d">除数</param>
        /// <returns>包含结果的新的Tensor</returns>
        public static Tensor Divide(Tensor t, double d)
        {
            return t.Clone().Divide(d);
        }

        /// <summary>
        /// 用d去除以Tensor中的每个元素，结果返回为新的Tensor
        /// </summary>
        /// <param name="d">被除数</param>
        /// <param name="t">除数</param>
        /// <returns>包含结果的新的Tensor</returns>
        public static Tensor Divide(double d, Tensor t)
        {
            return t.Clone().DivideBy(d);
        }

        /// <summary>
        /// 两个Tensor对应元素相处，结果返回为新的Tensor，要求两个Tensor结构一致
        /// </summary>
        /// <param name="a">被除数</param>
        /// <param name="b">除数</param>
        /// <returns>包含结果的新的Tensor</returns>
        public static Tensor DivideElementWise(Tensor a, Tensor b)
        {
            if (a.ElementCount == 1)
                return Divide(b, a.GetValue());
            if (b.ElementCount == 1)
                return Divide(a, b.GetValue());

            return a.Clone().DivideElementWise(b);
        }

        public static Tensor operator /(Tensor t, double d)
        {
            return Divide(t, d);
        }

        public static Tensor operator /(double d, Tensor t)
        {
            return Divide(d, t);
        }

        #endregion

        #region Override

        public override bool Equals(object o)
        {
            if (!(o is Tensor tensor))
                return false;

            if (Rank != tensor.Rank)
                return false;

            for (int i = 0; i < Rank; i++)
            {
                if (shape[i] != shape[i])
                    return false;
            }

            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] != tensor.values[i])
                    return false;
            }

            return true;
        }

        public override string ToString()
        {
            if (Rank == 1)
            {
                var content = string.Join(", ", values);
                return $"[{content}]";
            }
            else
            {
                var result = new List<string>();
                for (int i = 0; i < shape[0]; i++)
                {
                    result.Add(GetTensorByDim1(i).ToString());
                }
                var content = string.Join(",\n", result);
                return $"[{content}]";
            }
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion

        #region Helper Methods

        private void InitTensor(int[] shape)
        {
            CheckShape(shape);
            this.shape = shape;
            SetDimensionSize();
        }

        private int[] GetShape()
        {
            var result = new int[shape.Length];
            //复制shape，防止被修改
            shape.CopyTo(result, 0);
            return result;
        }

        private static int GetTotalLength(int[] shape)
        {
            if (shape.Length == 0)
                return 1;

            var result = 1;
            for (int i = 0; i < shape.Length; i++)
            {
                result *= shape[i];
            }
            return result;
        }

        private void SetDimensionSize()
        {
            if (Rank <= 1)
                return;

            dimensionSize = new int[Rank - 1];
            for (int i = 0; i < dimensionSize.Length; i++)
            {
                var temp = 1;
                for (int j = i + 1; j < shape.Length; j++)
                {
                    temp *= shape[j];
                }
                dimensionSize[i] = temp;
            }
        }

        private int GetOffset(int[] index)
        {
            if (index.Length == 1)
                return index[0];

            var result = 0;
            for (int i = 0; i < dimensionSize.Length; i++)
            {
                result += dimensionSize[i] * index[i];
            }
            result += index[index.Length - 1];

            return result;
        }

        private int[] GetIndex(int offset)
        {
            var rest = offset;
            var result = new int[Rank];
            for (int i = 0; i < dimensionSize.Length; i++)
            {
                result[i] = rest / dimensionSize[i];
                rest = rest % dimensionSize[i];
            }
            result[Rank - 1] = rest;

            return result;
        }

        private static void CheckShape(int[] shape)
        {
            foreach (var item in shape)
            {
                if (item <= 0)
                    throw new TensorShapeException("Tensor shape must > 0 !");
            }
        }

        private static void CheckShape(int[] shape1, int[] shape2)
        {
            if (shape1.Length != shape2.Length)
                throw new TensorShapeException("Tensor rank are not same!");

            for (int i = 0; i < shape1.Length; i++)
            {
                if (shape1[i] != shape2[i])
                    throw new TensorShapeException("shape are not the same!");
            }
        }

        private static void CheckMultipleShape(Tensor a, Tensor b)
        {
            if (a.Rank != 2 || b.Rank != 2)
                throw new NotImplementedException("only suport multiple between scalar, vector and matrix!");

            if (a.shape[1] != b.shape[0])
                throw new TensorShapeException($"can't multiple matrix between {a.shape.ToString()} and {b.shape.ToString()}");
        }

        #endregion
    }
}