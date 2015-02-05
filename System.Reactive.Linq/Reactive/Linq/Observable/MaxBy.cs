﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

#if !NO_PERF
using System;
using System.Collections.Generic;

namespace System.Reactive.Linq.ObservableImpl
{
    /// <summary>
    /// Base implement class of the MaxBy branch of Observable class. 
    /// In this class we get a list of TSource elements with the max value of TKey type in terms of  Func<> and IComparer<>.
    /// </summary>
    class MaxBy<TSource, TKey> : Producer<IList<TSource>>
    {
        private readonly IObservable<TSource> _source;
        private readonly Func<TSource, TKey> _keySelector;
        private readonly IComparer<TKey> _comparer;

        public MaxBy(IObservable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer)
        {
            _source = source;
            _keySelector = keySelector;
            _comparer = comparer;
        }

        protected override IDisposable Run(IObserver<IList<TSource>> observer, IDisposable cancel, Action<IDisposable> setSink)
        {
            var sink = new _(this, observer, cancel);
            setSink(sink);
            return _source.SubscribeSafe(sink);
        }

        /// <summary>
        /// The outline to  realize the MaxBy function. 
        /// 
        /// Initialize the guard parameter _lastKey which indicate the max key value.
        /// Foreach element value in the observable sequence () 
        ///     calculate the value key=_keySelector(value)
        ///     compare  key and _lastKey,  comparison = _comparer.Compare(key, _lastKey);
        ///     if comparison > 0
        ///         replace the guard parameter _lastKey=Key
        ///         clear the result list
        ///     if comparison >= 0
        ///         add new element to result list.
        /// Return the result list in the final. Note that this step  is always in the OnCompleted() function.
        /// 
        /// </summary>
        class _ : Sink<IList<TSource>>, IObserver<TSource>
        {
            private readonly MaxBy<TSource, TKey> _parent;
            // This function of _hasValue is to give the first key value generated by _keySelector of the sequence to _lastKey.
            private bool _hasValue;
            private TKey _lastKey;
            private List<TSource> _list;

            public _(MaxBy<TSource, TKey> parent, IObserver<IList<TSource>> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                _parent = parent;

                _hasValue = false;
                _lastKey = default(TKey);
                _list = new List<TSource>();
            }

            public void OnNext(TSource value)
            {
                var key = default(TKey);
                try
                {
                    key = _parent._keySelector(value);
                }
                catch (Exception ex)
                {
                    base._observer.OnError(ex);
                    base.Dispose();
                    return;
                }

                var comparison = 0;

                if (!_hasValue)
                {
                    _hasValue = true;
                    _lastKey = key;
                }
                else
                {
                    try
                    {
                        comparison = _parent._comparer.Compare(key, _lastKey);
                    }
                    catch (Exception ex)
                    {
                        base._observer.OnError(ex);
                        base.Dispose();
                        return;
                    }
                }

                if (comparison > 0)
                {
                    _lastKey = key;
                    _list.Clear();
                }

                if (comparison >= 0)
                {
                    _list.Add(value);
                }
            }

            public void OnError(Exception error)
            {
                base._observer.OnError(error);
                base.Dispose();
            }

            public void OnCompleted()
            {
                base._observer.OnNext(_list);
                base._observer.OnCompleted();
                base.Dispose();
            }
        }
    }
}
#endif