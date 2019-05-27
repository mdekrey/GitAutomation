import ResizeObserver from "resize-observer-polyfill";
import {
  useCallback,
  useState,
  useLayoutEffect,
  useRef,
  useEffect,
} from "react";

function getSize(el: Element | undefined | null) {
  if (!el) {
    return {
      width: 0,
      height: 0,
    };
  }

  return {
    width: el.clientWidth,
    height: el.clientHeight,
  };
}

export function useComponentSize<T extends Element>() {
  const [resizer, setResizer] = useState<ResizeObserver>();
  const ref = useRef<T | null>(null);
  const [ComponentSize, setComponentSize] = useState(
    getSize(ref ? ref.current : null)
  );

  const handleResize = useCallback(
    function handleResize() {
      if (ref.current) {
        const size = getSize(ref.current);
        setTimeout(() => setComponentSize(size), 0);
      }
    },
    [ref]
  );

  useEffect(() => {
    const resizer = new ResizeObserver(handleResize);
    setResizer(resizer);
    return () => resizer.disconnect();
  }, [handleResize]);

  useLayoutEffect(() => {
    const el = ref.current;
    if (!el || !resizer) {
      return;
    }

    handleResize();

    resizer.observe(el);
    const svgParent =
      el instanceof SVGElement && el.ownerSVGElement
        ? el.ownerSVGElement.parentElement
        : el.parentElement;
    if (svgParent) {
      resizer.observe(svgParent);
    }

    return () => {
      resizer.unobserve(el);
      if (svgParent) {
        resizer.unobserve(svgParent);
      }
    };
    // eslint-disable-next-line
  }, [ref.current, resizer, handleResize]);

  return { ...ComponentSize, ref };
}
