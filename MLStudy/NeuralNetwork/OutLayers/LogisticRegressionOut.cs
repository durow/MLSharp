﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MLStudy
{
    public class LogisticRegressionOut : LinearRegressionOut
    {
        public LogisticRegressionOut(int inputFeatures):base(inputFeatures)
        { }

        public override Matrix Forward(Matrix input)
        {
            base.Forward(input);
            ForwardOutput = ForwardOutput.ApplyFunction(Functions.Sigmoid);
            return ForwardOutput;
        }

        public override double GetLoss(Matrix yHat, Vector y)
        {
            var v = yHat.ToVector();
            return LossFunctions.Logistic(v, y);
        }

        public override double GetError(Vector y)
        {
            var yHat = GetPredict();
            return LossFunctions.ErrorPercent(yHat, y);
        }

        public override Vector GetPredict()
        {
            return (ForwardOutput - 0.5)
                .ApplyFunction(Functions.Indicator)
                .ToVector();
        }
    }
}
