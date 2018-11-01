﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MLStudy
{
    public class MatrixOperations
    {
        public static int MaxThreads
        {
            get
            {
                return ParallelOptions.MaxDegreeOfParallelism;
            }
            set
            {
                ParallelOptions.MaxDegreeOfParallelism = value;
            }
        }
        public static ParallelOptions ParallelOptions { get; private set; } = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
        public static MatrixOperations Instance { get; private set; } = new MatrixOperations();

        public static void UseSequence()
        {
            Instance = new MatrixOperations();
        }

        public static void UseParallel()
        { }

        #region Add

        public Vector Add(Vector v, double b)
        {
            var result = new double[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                result[i] = v[i] + b;
            }
            return new Vector(result);
        }

        public Vector Add(Vector a, Vector b)
        {
            CheckVectorLength(a, b);

            var result = new double[a.Length];
            for (int i = 0; i < a.Length; i++)
            {
                result[i] = a[i] + b[i];
            }
            return new Vector(result);
        }

        public virtual Matrix Add(Matrix m, double b)
        {
            var result = new double[m.Rows, m.Columns];
            for (int i = 0; i < m.Rows; i++)
            {
                for (int j = 0; j < m.Columns; j++)
                {
                    result[i, j] = m[i, j] + b;
                }
            }
            return new Matrix(result);
        }

        public virtual Matrix Add(Matrix a, Vector b)
        {
            if (a.Rows != b.Length)
                throw new Exception("not the same size!");

            var result = new Matrix(a.Rows, a.Columns);
            for (int i = 0; i < a.Columns; i++)
            {
                for (int j = 0; j < a.Rows; j++)
                {
                    result[j, i] = a[j, i] + b[j];
                }
            }

            return result;
        }

        public virtual Matrix Add(Vector a, Matrix b)
        {
            if (b.Columns != a.Length)
                throw new Exception("not the same size!");

            var result = new Matrix(b.Rows, b.Columns);
            for (int i = 0; i < b.Rows; i++)
            {
                for (int j = 0; j < b.Columns; j++)
                {
                    result[i, j] = a[j] + b[i, j];
                }
            }

            return result;
        }

        public virtual Matrix Add(Matrix a, Matrix b)
        {
            CheckMatrixShape(a, b);

            var result = new double[a.Rows, a.Columns];
            for (int i = 0; i < a.Rows; i++)
            {
                for (int j = 0; j < a.Columns; j++)
                {
                    result[i, j] = a[i, j] + b[i, j];
                }
            }
            return new Matrix(result);
        }

        #endregion


        #region Minus

        public Vector Minus(Vector v, double b)
        {
            var result = new double[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                result[i] = v[i] - b;
            }
            return new Vector(result);
        }

        public Vector Minus(double b, Vector v)
        {
            var result = new double[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                result[i] = b - v[i];
            }
            return new Vector(result);
        }

        public Vector Minus(Vector a, Vector b)
        {
            CheckVectorLength(a, b);

            var result = new double[a.Length];
            for (int i = 0; i < a.Length; i++)
            {
                result[i] = a[i] - b[i];
            }
            return new Vector(result);
        }

        public virtual Matrix Minus(Matrix m, double b)
        {
            var result = new double[m.Rows, m.Columns];
            for (int i = 0; i < m.Rows; i++)
            {
                for (int j = 0; j < m.Columns; j++)
                {
                    result[i, j] = m[i, j] - b;
                }
            }
            return new Matrix(result);
        }

        public virtual Matrix Minus(double b, Matrix m)
        {
            var result = new double[m.Rows, m.Columns];
            for (int i = 0; i < m.Rows; i++)
            {
                for (int j = 0; j < m.Columns; j++)
                {
                    result[i, j] = b - m[i, j];
                }
            }
            return new Matrix(result);
        }

        public virtual Matrix Minus(Matrix a, Vector b)
        {
            if (a.Rows != b.Length)
                throw new Exception("not the same size!");

            var result = new Matrix(a.Rows, a.Columns);
            for (int i = 0; i < a.Columns; i++)
            {
                for (int j = 0; j < a.Rows; j++)
                {
                    result[j, i] = a[j, i] - b[j];
                }
            }

            return result;
        }

        public virtual Matrix Minus(Vector a, Matrix b)
        {
            if (b.Columns != a.Length)
                throw new Exception("not the same size!");

            var result = new Matrix(b.Rows, b.Columns);
            for (int i = 0; i < b.Rows; i++)
            {
                for (int j = 0; j < b.Columns; j++)
                {
                    result[i, j] = a[j] - b[i, j];
                }
            }

            return result;
        }

        public virtual Matrix Minus(Matrix a, Matrix b)
        {
            CheckMatrixShape(a, b);

            var result = new double[a.Rows, a.Columns];
            for (int i = 0; i < a.Rows; i++)
            {
                for (int j = 0; j < a.Columns; j++)
                {
                    result[i, j] = a[i, j] - b[i, j];
                }
            }
            return new Matrix(result);
        }

        #endregion


        #region Multiple

        public Vector Multiple(Vector v, double b)
        {
            var result = new double[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                result[i] = v[i] * b;
            }
            return new Vector(result);
        }

        public double MultipleAsMatrix(Vector a, Vector b)
        {
            CheckVectorLength(a, b);

            var result = new double[a.Length];
            for (int i = 0; i < a.Length; i++)
            {
                result[i] = a[i] * b[i];
            }
            return result.Sum();
        }

        public Vector Multiple(Vector a, Vector b)
        {
            CheckVectorLength(a, b);

            var result = new double[a.Length];
            for (int i = 0; i < a.Length; i++)
            {
                result[i] = a[i] * b[i];
            }
            return new Vector(result);
        }

        public virtual Matrix Multiple(Matrix m, double b)
        {
            var result = new double[m.Rows, m.Columns];
            for (int i = 0; i < m.Rows; i++)
            {
                for (int j = 0; j < m.Columns; j++)
                {
                    result[i, j] = m[i, j] * b;
                }
            }
            return new Matrix(result);
        }

        public virtual Matrix Multiple(Matrix a, Vector v)
        {
            var m = v.ToMatrix(true);
            return Multiple(a, m);
        }

        public virtual Matrix Multiple(Vector v, Matrix a)
        {
            var m = v.ToMatrix();
            return Multiple(m, a);
        }

        public virtual Matrix Multiple(Matrix a, Matrix b)
        {
            if (a.Columns != b.Rows)
                throw new Exception($"a.Columns={a.Columns} and b.Rows={b.Rows} are not equal!");

            var result = new double[a.Rows, b.Columns];
            for (int i = 0; i < a.Rows; i++)
            {
                for (int j = 0; j < b.Columns; j++)
                {
                    result[i, j] = 0;
                    for (int k = 0; k < a.Columns; k++)
                    {
                        result[i, j] += (a[i, k] * b[k, j]);
                    }
                }
            }
            return new Matrix(result);
        }

        public virtual Matrix MultipleElementWise(Matrix a, Matrix b)
        {
            CheckMatrixShape(a, b);

            var result = a.GetSameShape();
            for (int i = 0; i < a.Rows; i++)
            {
                for (int j = 0; j < a.Columns; j++)
                {
                    result[i, j] = a[i, j] * b[i, j];
                }
            }
            return result;
        }

        #endregion


        #region Divide

        public Vector Divide(Vector v, double b)
        {
            var result = new double[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                result[i] = v[i] / b;
            }
            return new Vector(result);
        }

        public Vector Divide(double b, Vector v)
        {
            var result = new double[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                result[i] = b / v[i];
            }
            return new Vector(result);
        }

        public virtual Matrix Divide(Matrix m, double b)
        {
            var result = new double[m.Rows, m.Columns];
            for (int i = 0; i < m.Rows; i++)
            {
                for (int j = 0; j < m.Columns; j++)
                {
                    result[i, j] = m[i, j] / b;
                }
            }
            return new Matrix(result);
        }

        public virtual Matrix Divide(double b, Matrix m)
        {
            var result = new double[m.Rows, m.Columns];
            for (int i = 0; i < m.Rows; i++)
            {
                for (int j = 0; j < m.Columns; j++)
                {
                    result[i, j] = b / m[i, j];
                }
            }
            return new Matrix(result);
        }

        #endregion

        #region Helper Functions

        public void CheckVectorLength(Vector a, Vector b)
        {
            if (a.Length != b.Length)
                throw new Exception($"vector a.Length={a.Length} and b.Length={b.Length} are not the equal!");
        }

        public void CheckMatrixShape(Matrix a, Matrix b)
        {
            if (a.Rows != b.Rows || a.Columns != b.Columns)
                throw new Exception($"matrix shape a:[{a.Rows},{a.Columns}] and b:[{b.Rows},{b.Columns}] are not equal!");
        }

        #endregion
    }
}