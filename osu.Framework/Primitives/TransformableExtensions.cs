// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Primitives.Transforms;
using System;

namespace osu.Framework.Primitives
{
    public static class TransformableExtensions
    {
        /// <summary>
        /// Transforms a given property or field member of a given <see cref="ITransformable"/> <typeparamref name="TThis"/> to <paramref name="newValue"/>.
        /// The value of the given member is smoothly changed over time using the given <paramref name="easing"/> for tweening.
        /// </summary>
        /// <typeparam name="TThis">The type of the <see cref="ITransformable"/> to apply the <see cref="Transform{TValue, T}"/> to.</typeparam>
        /// <typeparam name="TValue">The value type which is being transformed.</typeparam>
        /// <param name="t">The <see cref="ITransformable"/> to apply the <see cref="Transform{TValue, T}"/> to.</param>
        /// <param name="propertyOrFieldName">The property or field name of the member ot <typeparamref name="TThis"/> to transform.</param>
        /// <param name="newValue">The value to transform to.</param>
        /// <param name="duration">The transform duration.</param>
        /// <param name="easing">The transform easing to be used for tweening.</param>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<TThis> TransformTo<TThis, TValue>(this TThis t, string propertyOrFieldName, TValue newValue, double duration = 0, Easing easing = Easing.None)
            where TThis : class, ITransformable
            => t.TransformTo(t.MakeTransform(propertyOrFieldName, newValue, duration, new DefaultEasingFunction(easing)));

        /// <summary>
        /// Transforms a given property or field member of a given <see cref="ITransformable"/> <typeparamref name="TThis"/> to <paramref name="newValue"/>.
        /// The value of the given member is smoothly changed over time using the given <paramref name="easing"/> for tweening.
        /// </summary>
        /// <typeparam name="TThis">The type of the <see cref="ITransformable"/> to apply the <see cref="Transform{TValue, T}"/> to.</typeparam>
        /// <typeparam name="TValue">The value type which is being transformed.</typeparam>
        /// <typeparam name="TEasing">The type of easing.</typeparam>
        /// <param name="t">The <see cref="ITransformable"/> to apply the <see cref="Transform{TValue, T}"/> to.</param>
        /// <param name="propertyOrFieldName">The property or field name of the member ot <typeparamref name="TThis"/> to transform.</param>
        /// <param name="newValue">The value to transform to.</param>
        /// <param name="duration">The transform duration.</param>
        /// <param name="easing">The transform easing to be used for tweening.</param>
        /// <param name="grouping">An optional grouping specification to be used when the same property may be touched by multiple transform types.</param>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<TThis> TransformTo<TThis, TValue, TEasing>(this TThis t, string propertyOrFieldName, TValue newValue, double duration, in TEasing easing, string grouping = null)
            where TThis : class, ITransformable
            where TEasing : IEasingFunction
            => t.TransformTo(t.MakeTransform(propertyOrFieldName, newValue, duration, easing, grouping));

        /// <summary>
        /// Applies a <see cref="Transform"/> to a given <see cref="ITransformable"/>.
        /// </summary>
        /// <typeparam name="TThis">The type of the <see cref="ITransformable"/> to apply the <see cref="Transform"/> to.</typeparam>
        /// <param name="t">The <see cref="ITransformable"/> to apply the <see cref="Transform{TValue, T}"/> to.</param>
        /// <param name="transform">The transform to use.</param>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<TThis> TransformTo<TThis>(this TThis t, Transform transform) where TThis : class, ITransformable
        {
            var result = new TransformSequence<TThis>(t);
            result.Add(transform);
            t.AddTransform(transform);
            return result;
        }

        /// <summary>
        /// Creates a <see cref="Transform{TValue, T}"/> for smoothly changing <paramref name="propertyOrFieldName"/>
        /// over time using the given <paramref name="easing"/> for tweening.
        /// <see cref="PopulateTransform{TValue, DefaultEasingFunction, TThis}"/> is invoked as part of this method.
        /// </summary>
        /// <typeparam name="TThis">The type of the <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> can be applied to.</typeparam>
        /// <typeparam name="TValue">The value type which is being transformed.</typeparam>
        /// <param name="t">The <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> will be applied to.</param>
        /// <param name="propertyOrFieldName">The property or field name of the member ot <typeparamref name="TThis"/> to transform.</param>
        /// <param name="newValue">The value to transform to.</param>
        /// <param name="duration">The transform duration.</param>
        /// <param name="easing">The transform easing to be used for tweening.</param>
        /// <param name="grouping">An optional grouping specification to be used when the same property may be touched by multiple transform types.</param>
        /// <returns>The resulting <see cref="Transform{TValue, T}"/>.</returns>
        public static Transform<TValue, DefaultEasingFunction, TThis> MakeTransform<TThis, TValue>(this TThis t, string propertyOrFieldName, TValue newValue, double duration = 0,
                                                                                                   Easing easing = Easing.None, string grouping = null)
            where TThis : class, ITransformable
            => t.MakeTransform(propertyOrFieldName, newValue, duration, new DefaultEasingFunction(easing), grouping);

        /// <summary>
        /// Creates a <see cref="Transform{TValue, T}"/> for smoothly changing <paramref name="propertyOrFieldName"/>
        /// over time using the given <paramref name="easing"/> for tweening.
        /// <see cref="PopulateTransform{TValue, TEasing, TThis}"/> is invoked as part of this method.
        /// </summary>
        /// <typeparam name="TThis">The type of the <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> can be applied to.</typeparam>
        /// <typeparam name="TValue">The value type which is being transformed.</typeparam>
        /// <typeparam name="TEasing">The type of easing.</typeparam>
        /// <param name="t">The <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> will be applied to.</param>
        /// <param name="propertyOrFieldName">The property or field name of the member ot <typeparamref name="TThis"/> to transform.</param>
        /// <param name="newValue">The value to transform to.</param>
        /// <param name="duration">The transform duration.</param>
        /// <param name="easing">The transform easing to be used for tweening.</param>
        /// <param name="grouping">An optional grouping specification to be used when the same property may be touched by multiple transform types.</param>
        /// <returns>The resulting <see cref="Transform{TValue, T}"/>.</returns>
        public static Transform<TValue, TEasing, TThis> MakeTransform<TThis, TEasing, TValue>(this TThis t, string propertyOrFieldName, TValue newValue, double duration, in TEasing easing, string grouping = null)
            where TThis : class, ITransformable
            where TEasing : IEasingFunction
            => t.PopulateTransform(new TransformCustom<TValue, TEasing, TThis>(propertyOrFieldName, grouping), newValue, duration, easing);

        /// <summary>
        /// Populates a newly created <see cref="Transform{TValue, T}"/> with necessary values.
        /// All <see cref="Transform{TValue, T}"/>s must be populated by this method prior to being used.
        /// </summary>
        /// <typeparam name="TThis">The type of the <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> can be applied to.</typeparam>
        /// <typeparam name="TValue">The value type which is being transformed.</typeparam>
        /// <param name="t">The <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> will be applied to.</param>
        /// <param name="transform">The transform to populate.</param>
        /// <param name="newValue">The value to transform to.</param>
        /// <param name="duration">The transform duration.</param>
        /// <param name="easing">The transform easing to be used for tweening.</param>
        /// <returns>The populated <paramref name="transform"/>.</returns>
        public static Transform<TValue, DefaultEasingFunction, TThis> PopulateTransform<TValue, TThis>(this TThis t, Transform<TValue, DefaultEasingFunction, TThis> transform, TValue newValue,
                                                                                                       double duration = 0, Easing easing = Easing.None)
            where TThis : class, ITransformable
            => t.PopulateTransform(transform, newValue, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Populates a newly created <see cref="Transform{TValue, T}"/> with necessary values.
        /// All <see cref="Transform{TValue, T}"/>s must be populated by this method prior to being used.
        /// </summary>
        /// <typeparam name="TThis">The type of the <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> can be applied to.</typeparam>
        /// <typeparam name="TValue">The value type which is being transformed.</typeparam>
        /// <typeparam name="TEasing">The type of easing.</typeparam>
        /// <param name="t">The <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> will be applied to.</param>
        /// <param name="transform">The transform to populate.</param>
        /// <param name="newValue">The value to transform to.</param>
        /// <param name="duration">The transform duration.</param>
        /// <param name="easing">The transform easing to be used for tweening.</param>
        /// <returns>The populated <paramref name="transform"/>.</returns>
        public static Transform<TValue, TEasing, TThis> PopulateTransform<TValue, TEasing, TThis>(this TThis t, Transform<TValue, TEasing, TThis> transform, TValue newValue, double duration,
                                                                                                  in TEasing easing)
            where TThis : class, ITransformable
            where TEasing : IEasingFunction
        {
            if (duration < 0)
                throw new ArgumentOutOfRangeException(nameof(duration), $"{nameof(duration)} must be positive.");

            if (transform.Target != null)
                throw new InvalidOperationException($"May not {nameof(PopulateTransform)} the same {nameof(Transform<TValue, TThis>)} more than once.");

            transform.Target = t;

            double startTime = t.TransformStartTime;

            transform.StartTime = startTime;
            transform.EndTime = startTime + duration;
            transform.EndValue = newValue;
            transform.Easing = easing;

            return transform;
        }

        /// <summary>
        /// Applies <paramref name="childGenerators"/> via TransformSequence.Append(IEnumerable{Generator})/>.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> can be applied to.</typeparam>
        /// <param name="transformable">The <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> will be applied to.</param>
        /// <param name="childGenerators">The optional Generators for <see cref="TransformSequence{T}"/>s to be appended.</param>
        /// <returns>This <see cref="TransformSequence{T}"/>.</returns>
        public static TransformSequence<T> Animate<T>(this T transformable, params TransformSequence<T>.Generator[] childGenerators) where T : class, ITransformable =>
            transformable.Delay(0, childGenerators);

        /// <summary>
        /// Advances the start time of future appended <see cref="TransformSequence{T}"/>s by <paramref name="delay"/> milliseconds.
        /// Then, <paramref name="childGenerators"/> are appended via TransformSequence.Append(IEnumerable{Generator})/>.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> can be applied to.</typeparam>
        /// <param name="transformable">The <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> will be applied to.</param>
        /// <param name="delay">The delay to advance the start time by.</param>
        /// <param name="childGenerators">The optional Generators for <see cref="TransformSequence{T}"/>s to be appended.</param>
        /// <returns>This <see cref="TransformSequence{T}"/>.</returns>
        public static TransformSequence<T> Delay<T>(this T transformable, double delay, params TransformSequence<T>.Generator[] childGenerators) where T : class, ITransformable =>
            new TransformSequence<T>(transformable).Delay(delay, childGenerators);

        /// <summary>
        /// Returns a <see cref="TransformSequence{T}"/> which waits for all existing transforms to finish.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> which has a delay waiting for all transforms to be completed.</returns>
        public static TransformSequence<T> DelayUntilTransformsFinished<T>(this T transformable)
            where T : Transformable =>
            transformable.Delay(Math.Max(0, transformable.LatestTransformEndTime - transformable.Time.Current));

        /// <summary>
        /// Append a looping <see cref="TransformSequence{T}"/> to this <see cref="TransformSequence{T}"/>.
        /// All <see cref="Transform"/>s generated by <paramref name="childGenerators"/> are appended to
        /// this <see cref="TransformSequence{T}"/> and then repeated <paramref name="numIters"/> times
        /// with <paramref name="pause"/> milliseconds between iterations.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> can be applied to.</typeparam>
        /// <param name="transformable">The <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> will be applied to.</param>
        /// <param name="pause">The pause between iterations in milliseconds.</param>
        /// <param name="numIters">The number of iterations.</param>
        /// <param name="childGenerators">The functions to generate the <see cref="TransformSequence{T}"/>s to be looped.</param>
        /// <returns>This <see cref="TransformSequence{T}"/>.</returns>
        public static TransformSequence<T> Loop<T>(this T transformable, double pause, int numIters, params TransformSequence<T>.Generator[] childGenerators)
            where T : class, ITransformable =>
            transformable.Delay(0).Loop(pause, numIters, childGenerators);

        /// <summary>
        /// Append a looping <see cref="TransformSequence{T}"/> to this <see cref="TransformSequence{T}"/>.
        /// All <see cref="Transform"/>s generated by <paramref name="childGenerators"/> are appended to
        /// this <see cref="TransformSequence{T}"/> and then repeated indefinitely with <paramref name="pause"/>
        /// milliseconds between iterations.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> can be applied to.</typeparam>
        /// <param name="transformable">The <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> will be applied to.</param>
        /// <param name="pause">The pause between iterations in milliseconds.</param>
        /// <param name="childGenerators">The functions to generate the <see cref="TransformSequence{T}"/>s to be looped.</param>
        /// <returns>This <see cref="TransformSequence{T}"/>.</returns>
        public static TransformSequence<T> Loop<T>(this T transformable, double pause, params TransformSequence<T>.Generator[] childGenerators)
            where T : class, ITransformable =>
            transformable.Delay(0).Loop(pause, childGenerators);

        /// <summary>
        /// Append a looping <see cref="TransformSequence{T}"/> to this <see cref="TransformSequence{T}"/>.
        /// All <see cref="Transform"/>s generated by <paramref name="childGenerators"/> are appended to
        /// this <see cref="TransformSequence{T}"/> and then repeated indefinitely.
        /// milliseconds between iterations.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> can be applied to.</typeparam>
        /// <param name="transformable">The <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> will be applied to.</param>
        /// <param name="childGenerators">The functions to generate the <see cref="TransformSequence{T}"/>s to be looped.</param>
        /// <returns>This <see cref="TransformSequence{T}"/>.</returns>
        public static TransformSequence<T> Loop<T>(this T transformable, params TransformSequence<T>.Generator[] childGenerators)
            where T : class, ITransformable =>
            transformable.Delay(0).Loop(childGenerators);

        /// <summary>
        /// Append a looping <see cref="TransformSequence{T}"/> to this <see cref="TransformSequence{T}"/> to repeat indefinitely with <paramref name="pause"/>
        /// milliseconds between iterations.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> can be applied to.</typeparam>
        /// <param name="transformable">The <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> will be applied to.</param>
        /// <param name="pause">The pause between iterations in milliseconds.</param>
        /// <returns>This <see cref="TransformSequence{T}"/>.</returns>
        public static TransformSequence<T> Loop<T>(this T transformable, double pause = 0)
            where T : class, ITransformable =>
            transformable.Delay(0).Loop(pause);
    }
}
