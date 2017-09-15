import { Observable, Subscription, Subject } from "rxjs";
import { Selection } from "d3-selection";

import { RoutingComponent } from "../utils/routing-component";
import {
  rxEvent,
  fnSelect,
  rxDatum,
  rxData,
  d3element
} from "../utils/presentation/d3-binding";
import { runBranchData } from "./data";
import { buildBranchCheckListing } from "./branch-check-listing";
import { bindSaveButton } from "./bind-save-button";
import {
  consolidateMerged,
  promoteServiceLine,
  deleteBranch,
  detectUpstream
} from "../api/basics";

export const manage = (
  container: Observable<Selection<HTMLElement, {}, null, undefined>>
): RoutingComponent => state =>
  container
    .do(elem =>
      elem.html(`
  <a data-locator="home">Home</a>
  <h1 data-locator="branch-name"></h1>
  <h3>Settings</h3>
  <label>
      <input type="checkbox" data-locator="recreate-from-upstream" />
      Recreate from Upstream
  </label>
  <label>
      Branch Type
      <select data-locator="branch-type">
        <option value="Feature">Feature</option>
        <option value="ReleaseCandidate">Release Candidate</option>
        <option value="ServiceLine">Service Line</option>
        <option value="Infrastructure">Infrastructure</option>
        <option value="Integration">Integration</option>
        <option value="Hotfix">Hotfix</option>
      </select>
  </label>
  <h3>Other Branches</h3>

  <table>
      <thead>
        <tr>
          <td></td>
          <th>Downstream</th>
          <th>Upstream<br/><a data-locator="detect-upstream">Detect Upstream Branches</a></th>
        </tr>
      </thead>
      <tbody data-locator="other-branches">
      </tbody>
  </table>
  <button type="button" data-locator="reset">Reset</button>
  <button type="button" data-locator="home">Cancel</button>
  <button type="button" data-locator="save">Save</button>
  <h3>Actual Branches</h3>
  <ul data-locator="grouped-branches"></ul>

  <h3>Release to Service Line</h3>
  <label>
    <span>Approved Branch</span>
    <select data-locator="approved-branch"></select>
  </label>
  <label>
    <span>Service Line Branch</span>
    <input type="text" data-locator="service-line-branch" />
  </label>
  <label>
    <span>Release Tag</span>
    <input type="text" data-locator="release-tag" />
  </label>
  <button type="button" data-locator="promote-service-line">Release to Service Line</button>

  <h3>Consolidate Merged</h3>
  <label>
    <span>Consolidate Into</span>
    <select data-locator="consolidate-target-branch"></select>
  </label>
  <ul data-locator="consolidate-original-branches"></ul>
  <button type="button" data-locator="consolidate-branch">Consolidate Branch</button>

  <h3>Delete Branch</h3>
  <p>This action cannot be undone.</p>
  <button type="button" data-locator="delete-branch">Delete</button>
`)
    )
    .publishReplay(1)
    .refCount()
    .let(container =>
      Observable.create(() => {
        const subscription = new Subscription();
        const branchName = state.state.remainingPath!;
        const reload = new Subject<null>();

        // go home
        subscription.add(
          rxEvent({
            target: container.map(body =>
              body.selectAll('[data-locator="home"]')
            ),
            eventName: "click"
          }).subscribe(() =>
            state.navigate({ url: "/", replaceCurentHistory: false })
          )
        );

        // reset
        const reset = rxEvent({
          target: container.map(body =>
            body.selectAll('[data-locator="reset"]')
          ),
          eventName: "click"
        })
          .map(() => null)
          .publish()
          .refCount()
          .startWith(null);

        const branchData = runBranchData(branchName!, reload);
        subscription.add(branchData.subscription);

        subscription.add(
          bindSaveButton(
            branchName,
            '[data-locator="save"]',
            container,
            branchData.state,
            () => reload.next(null)
          )
        );

        // display branch name
        subscription.add(
          container
            .map(fnSelect(`[data-locator="branch-name"]`))
            .let(rxDatum(Observable.of(branchName)))
            .subscribe(target => target.text(data => data))
        );

        const branchList = branchData.state
          .map(state =>
            state.branches.filter(({ branch }) => branch !== branchName)
          )
          .combineLatest(reset, _ => _);

        subscription.add(
          container
            .map(e => e.select(`[data-locator="recreate-from-upstream"]`))
            .switchMap(e =>
              branchData.state
                .map(d => d.recreateFromUpstream)
                .map(d => e.datum(d))
            )
            .subscribe(target => {
              target.property("checked", value => value);
            })
        );

        subscription.add(
          container
            .map(e => e.select(`[data-locator="branch-type"]`))
            .switchMap(e =>
              branchData.state.map(d => d.branchType).map(d => e.datum(d))
            )
            .subscribe(target => {
              target.property("value", value => value);
            })
        );

        // display downstream branches
        subscription.add(
          rxData(
            container.map(fnSelect(`[data-locator="other-branches"]`)),
            branchList,
            data => data.branch
          )
            .bind(buildBranchCheckListing())
            .subscribe()
        );

        subscription.add(
          rxData(
            container.map(fnSelect(`[data-locator="grouped-branches"]`)),
            branchData.state.map(branch => branch.branchNames)
          )
            .bind({
              selector: "li",
              onCreate: selection => selection.append<HTMLLIElement>("li"),
              onEach: selection => selection.text(data => data)
            })
            .subscribe()
        );

        subscription.add(
          rxData(
            container.map(fnSelect(`[data-locator="approved-branch"]`)),
            branchData.state.map(branch => branch.branchNames)
          )
            .bind({
              selector: "option",
              onCreate: selection =>
                selection.append<HTMLOptionElement>("option"),
              onEach: selection =>
                selection.text(data => data).attr("value", data => data)
            })
            .subscribe()
        );

        subscription.add(
          rxData(
            container.map(
              fnSelect(`[data-locator="consolidate-target-branch"]`)
            ),
            branchList.map(branches =>
              branches.filter(branch => !branch.isUpstream)
            ),
            data => data.branch
          )
            .bind({
              selector: "option",
              onCreate: selection =>
                selection.append<HTMLOptionElement>("option"),
              onEach: selection =>
                selection
                  .text(data => data.branch)
                  .attr("value", data => data.branch)
            })
            .subscribe()
        );
        subscription.add(
          rxData(
            container.map(
              fnSelect(`[data-locator="consolidate-original-branches"]`)
            ),
            branchList.map(branches =>
              [branchName].concat(
                branches
                  .filter(branch => branch.isUpstream)
                  .map(branch => branch.branch)
              )
            ),
            data => data
          )
            .bind({
              selector: "li",
              onCreate: selection => selection.append<HTMLLIElement>("li"),
              onEnter: selection =>
                selection.html(
                  `<label>
                    <input type="checkbox" data-locator="consolidate-original-branch" />
                    <span />
                  </label>`
                ),
              onEach: selection => {
                selection.select("span").text(b => b);
                selection.select("input").attr("data-branch", b => b);
              }
            })
            .subscribe()
        );

        subscription.add(
          rxEvent({
            target: container.map(fnSelect(`[data-locator="detect-upstream"]`)),
            eventName: "click"
          })
            .withLatestFrom(
              container.map(fnSelect(`[data-locator="other-branches"]`)),
              (_, elem) => elem
            )
            .subscribe(elements => {
              detectUpstream(branchName).subscribe(branchNames =>
                branchNames.forEach(upstreamBranchName =>
                  elements
                    .select(
                      `[data-locator="upstream-branches"] [data-locator="check"][data-branch="${upstreamBranchName}"]`
                    )
                    .property("checked", true)
                )
              );
            })
        );

        subscription.add(
          rxEvent({
            target: container.map(
              fnSelect(`[data-locator="promote-service-line"]`)
            ),
            eventName: "click"
          })
            .switchMap(_ =>
              Observable.combineLatest(
                container
                  .map(fnSelect(`[data-locator="approved-branch"]`))
                  .map(sl => sl.property("value") as string),
                container
                  .map(fnSelect(`[data-locator="service-line-branch"]`))
                  .map(sl => sl.property("value") as string),
                container
                  .map(fnSelect(`[data-locator="release-tag"]`))
                  .map(sl => sl.property("value") as string)
              ).map(([releaseCandidate, serviceLine, tagName]) => ({
                releaseCandidate,
                serviceLine,
                tagName
              }))
            )
            .take(1)
            .switchMap(promoteServiceLine)
            .subscribe(response => {
              state.navigate({ url: "/", replaceCurentHistory: false });
            })
        );

        subscription.add(
          rxEvent({
            target: container.map(
              fnSelect(`[data-locator="consolidate-branch"]`)
            ),
            eventName: "click"
          })
            .switchMap(_ =>
              Observable.combineLatest(
                container
                  .map(fnSelect(`[data-locator="consolidate-target-branch"]`))
                  .map(sl => sl.property("value") as string),
                container
                  .map(elem =>
                    elem.selectAll<HTMLInputElement, any>(
                      `[data-locator="consolidate-original-branches"] [data-locator="consolidate-original-branch"]:checked`
                    )
                  )
                  .map(sl => sl.nodes())
                  .map(checkboxes =>
                    checkboxes
                      .map(d3element)
                      .map(checkbox => checkbox.attr("data-branch"))
                  )
              ).map(([targetBranch, originalBranches]) => ({
                targetBranch,
                originalBranches
              }))
            )
            .take(1)
            .switchMap(consolidateMerged)
            .subscribe(response => {
              state.navigate({ url: "/", replaceCurentHistory: false });
            })
        );

        subscription.add(
          rxEvent({
            target: container.map(fnSelect(`[data-locator="delete-branch"]`)),
            eventName: "click"
          })
            .map(() => branchName)
            .take(1)
            .switchMap(deleteBranch)
            .subscribe(response => {
              state.navigate({ url: "/", replaceCurentHistory: false });
            })
        );

        return subscription;
      })
    );
