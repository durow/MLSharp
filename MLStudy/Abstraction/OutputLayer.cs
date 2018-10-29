﻿using System;
using System.Collections.Generic;

namespace MLStudy
{
    public abstract class OutputLayer
    {
        //#region static

        //public static Dictionary<string, Type> Dict;

        //static OutputLayer()
        //{
        //    Dict = new Dictionary<string, Type>();
        //    Registor<LinearRegressionOut>("LinearRegression");
        //    Registor<LogisticRegressionOut>("LogisticRegression");
        //    Registor<SoftmaxOut>("Softmax");
        //}

        //public static void Registor<T>(string name) where T : OutputLayer
        //{
        //    Dict[name] = typeof(T);
        //}

        //public static OutputLayer Get(string name, int inputFeatures, int neuronCount)
        //{
        //    if (Dict.ContainsKey(name))
        //        return (OutputLayer)Activator.CreateInstance(Dict[name],);
        //    else
        //        return null;
        //}

        //#endregion

        public int InputFeatures { get; protected set; }
        public double LearningRate
        {
            get
            {
                return Optimizer.LearningRate;
            }
            set
            {
                Optimizer.LearningRate = value;
            }
        }
        public GradientOptimizer Optimizer { get; protected set; } = new GradientOptimizer();

        public Matrix ForwardInput { get; protected set; }
        public Matrix ForwardOutput { get; protected set; }
        public Matrix InputError { get; protected set; }
        public Matrix LinearError { get; protected set; }
        public double Loss { get; protected set; }

        public abstract Matrix Forward(Matrix input);

        public abstract Matrix Backward(Vector y);

        public abstract void AutoInitWeightsBias();
    }
}
