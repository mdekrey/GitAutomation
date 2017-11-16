import { Observable } from "../utils/rxjs";
import { Selection } from "d3-selection";
import { claims } from "./app-access";
import { ClaimDetails } from "../api/claim-details";
import { intersection } from "../utils/ramda";

type ActualElement = Element;
type AnySelection = Selection<ActualElement, {}, null, undefined>;

export const applySecurity = (currentClaims: ClaimDetails) => <
  T extends AnySelection
>(
  elem: T
) => {
  elem.selectAll(`[data-role]`).style("display", function(this: ActualElement) {
    const _this = this;
    if (_this !== null) {
      const roleNames = _this.getAttribute("data-role")!.split(/[ ,]/g);
      return intersection(currentClaims.roles, roleNames).length > 0
        ? null
        : "none";
    }
    return null;
  });
};

export const secured = function<T extends AnySelection>(
  target: Observable<T>
): Observable<T> {
  return claims.switchMap(currentClaims =>
    target.do(applySecurity(currentClaims))
  );
};
